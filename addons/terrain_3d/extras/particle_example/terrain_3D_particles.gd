# 版权所有 © 2025 Cory Petkovsek, Roope Palmroos 及贡献者。
#
# 这是 Terrain3D 粒子着色器的使用示例
# 使用方法：将 `Terrain3DParticles.tscn` 添加到你的场景并绑定地形节点
# 然后自定义参数、材质和着色器，根据需求扩展和修改

@tool
extends Node3D


#region 配置项
## 如果作为 Terrain3D 节点的子节点，会自动赋值
@export var terrain: Terrain3D:
	set(value):
		terrain = value
		_create_grid()  # 创建粒子网格


## 粒子实例之间的间距
@export_range(0.125, 2.0, 0.015625) var instance_spacing: float = 0.5:
	set(value):
		# 限制间距范围并按步长取整
		instance_spacing = clamp(round(value * 64.0) * 0.015625, 0.125, 2.0)
		# 重新计算每行粒子数和总粒子数
		rows = maxi(int(cell_width / instance_spacing), 1)
		amount = rows * rows
		_set_offsets()  # 更新粒子偏移量


## 单个网格单元格的宽度
@export_range(8.0, 256.0, 1.0) var cell_width: float = 32.0:
	set(value):
		cell_width = clamp(value, 8.0, 256.0)
		# 重新计算每行粒子数和总粒子数
		rows = maxi(int(cell_width / instance_spacing), 1)
		amount = rows * rows
		min_draw_distance = 1.0
		
		# 更新粒子的轴对齐包围盒（AABB）
		if terrain and terrain.data:
			var height_range: Vector2 = terrain.data.get_height_range()  # 获取地形高度范围
			var height: float = height_range[0] - height_range[1]       # 计算地形总高度
			var aabb: AABB = AABB()                                     # 创建包围盒
			aabb.size = Vector3(cell_width, height, cell_width)         # 设置包围盒尺寸
			aabb.position = aabb.size * -0.5                            # 居中包围盒
			aabb.position.y = height_range[1]                           # 对齐地形最低高度
			for p in particle_nodes:
				p.custom_aabb = aabb  # 给每个粒子节点设置包围盒
		_set_offsets()  # 更新粒子偏移量


## 网格宽度（必须是奇数）
## 值越高，剔除效果越好，粒子绘制范围越远
@export_range(1, 15, 2) var grid_width: int = 9:
	set(value):
		grid_width = value
		particle_count = 1
		min_draw_distance = 1.0
		_create_grid()  # 重建粒子网格


# 存储每行的粒子数量（仅序列化，不显示在编辑器）
@export_storage var rows: int = 1

# 单个网格的粒子总数
@export_storage var amount: int = 1:
	set(value):
		amount = value
		particle_count = value
		last_pos = Vector3.ZERO
		# 给所有粒子节点设置数量
		for p in particle_nodes:
			p.amount = amount


## 粒子固定更新帧率
@export_range(1, 256, 1) var process_fixed_fps: int = 30:
	set(value):
		process_fixed_fps = maxi(value, 1)
		# 给所有粒子节点设置帧率和预处理时间
		for p in particle_nodes:
			p.fixed_fps = process_fixed_fps
			p.preprocess = 1.0 / float(process_fixed_fps)


## 粒子处理材质参数（关联着色器材质）
@export var process_material: ShaderMaterial

## 每个粒子渲染使用的网格
@export var mesh: Mesh

## 粒子阴影投射模式
@export var shadow_mode: GeometryInstance3D.ShadowCastingSetting = (
		GeometryInstance3D.ShadowCastingSetting.SHADOW_CASTING_SETTING_ON):
	set(value):
		shadow_mode = value
		# 给所有粒子节点设置阴影模式
		for p in particle_nodes:
			p.cast_shadow = value


## 粒子网格的材质覆盖（优先级高于默认材质）
@export_custom(
	PROPERTY_HINT_RESOURCE_TYPE,
	"BaseMaterial3D,ShaderMaterial") var mesh_material_override: Material:
	set(value):
		mesh_material_override = value
		# 给所有粒子节点设置材质覆盖
		for p in particle_nodes:
			p.material_override = value


# 信息分组（仅显示，不可编辑）
@export_group("信息")
## 粒子绘制的最小距离
## 如果使用像素透明度淡出效果，此值为有效范围上限
@export var min_draw_distance: float = 1.0:
	set(value):
		min_draw_distance = float(cell_width * grid_width) * 0.5


## 显示当前总粒子数（由单元格宽度和实例间距计算得出）
@export var particle_count: int = 1:
	set(value):
		particle_count = amount * grid_width * grid_width


#endregion


# 粒子偏移量数组
var offsets: Array[Vector3]
# 相机上一帧位置（用于判断是否需要更新网格）
var last_pos: Vector3 = Vector3.ZERO
# 所有粒子节点的数组
var particle_nodes: Array[GPUParticles3D]


# 节点就绪时调用
func _ready() -> void:
	# 如果未手动绑定地形，尝试从父节点获取
	if not terrain:
		var parent: Node = get_parent()
		if parent is Terrain3D:
			terrain = parent
	_create_grid()  # 创建粒子网格


# 节点通知回调
func _notification(what: int) -> void:
	# 节点被删除前清理粒子网格
	if what == NOTIFICATION_PREDELETE:
		_destroy_grid()


# 物理帧更新（每帧调用）
func _physics_process(delta: float) -> void:
	if terrain:
		# 获取地形关联的相机
		var camera: Camera3D = terrain.get_camera()
		if camera:
			# 相机移动超过1单位时更新粒子网格位置
			if last_pos.distance_squared_to(camera.global_position) > 1.0:
				var pos: Vector3 = camera.global_position.snapped(Vector3.ONE)
				_position_grid(pos)  # 定位粒子网格
				# 给着色器传递相机位置参数
				RenderingServer.material_set_param(process_material.get_rid(), "camera_position", pos )
				last_pos = camera.global_position
		_update_process_parameters()  # 更新着色器参数
	else:
		# 无地形时停止物理更新
		set_physics_process(false)


# 创建粒子网格
func _create_grid() -> void:
	_destroy_grid()  # 先销毁旧网格
	if not terrain:
		return
	set_physics_process(true)  # 启用物理更新
	_set_offsets()             # 设置粒子偏移量
	
	# 获取地形高度范围并创建包围盒
	var hr: Vector2 = terrain.data.get_height_range()
	var height: float = hr.x - hr.y
	var aabb: AABB = AABB()
	aabb.size = Vector3(cell_width, height, cell_width)
	aabb.position = aabb.size * -0.5
	aabb.position.y = hr.y
	
	var half_grid: int = grid_width / 2
	# 遍历创建粒子节点（按网格排列）
	for x in range(-half_grid, half_grid + 1):
		for z in range(-half_grid, half_grid + 1):
			var particle_node = GPUParticles3D.new()
			# 设置粒子基础属性
			particle_node.lifetime = 600.0          # 粒子生命周期（10分钟）
			particle_node.amount = amount           # 粒子数量
			particle_node.explosiveness = 1.0       # 瞬间生成所有粒子
			particle_node.amount_ratio = 1.0        # 粒子显示比例
			particle_node.process_material = process_material  # 处理材质
			particle_node.draw_pass_1 = mesh        # 渲染网格
			particle_node.speed_scale = 1.0         # 速度缩放
			particle_node.custom_aabb = aabb        # 自定义包围盒
			particle_node.cast_shadow = shadow_mode # 阴影模式
			particle_node.fixed_fps = process_fixed_fps # 固定帧率
			# 防止相机快速移动时的网格对齐误差
			particle_node.preprocess = 1.0 / float(process_fixed_fps)
			
			# 设置材质覆盖
			if mesh_material_override:
				particle_node.material_override = mesh_material_override
			
			# 使用固定随机种子（避免粒子闪烁）
			particle_node.use_fixed_seed = true
			# 所有粒子节点使用相同种子（保证随机效果一致）
			if (x > -half_grid and z > -half_grid):
				particle_node.seed = particle_nodes[0].seed
			
			# 添加并启用粒子节点
			self.add_child(particle_node)
			particle_node.emitting = true
			particle_nodes.push_back(particle_node)
	last_pos = Vector3.ZERO


# 设置粒子偏移量（按网格计算）
func _set_offsets() -> void:
	var half_grid: int = grid_width / 2
	offsets.clear()
	# 遍历计算每个粒子的偏移量
	for x in range(-half_grid, half_grid + 1):
		for z in range(-half_grid, half_grid + 1):
			var offset := Vector3(
				float(x * rows) * instance_spacing,
				0.0,
				float(z * rows) * instance_spacing
			)
			offsets.append(offset)


# 销毁粒子网格（清理资源）
func _destroy_grid() -> void:
	# 遍历销毁所有粒子节点
	for node: GPUParticles3D in particle_nodes:
		if is_instance_valid(node):
			node.queue_free()
	particle_nodes.clear()


# 定位粒子网格（跟随相机移动）
func _position_grid(pos: Vector3) -> void:
	# 遍历更新每个粒子节点的位置
	for i in particle_nodes.size():
		var node: GPUParticles3D = particle_nodes[i]
		# 计算粒子位置（对齐到整数避免抖动）
		var snap = Vector3(pos.x, 0, pos.z).snapped(Vector3.ONE) + offsets[i]
		node.global_position = (snap / instance_spacing).round() * instance_spacing
		node.reset_physics_interpolation()  # 重置物理插值
		node.restart(true)  # 重启粒子（保留随机种子）


# 更新着色器处理参数（传递地形数据给粒子）
func _update_process_parameters() -> void:
	if process_material:
		var process_rid: RID = process_material.get_rid()
		if terrain and process_rid.is_valid():
			# 传递地形材质参数
			RenderingServer.material_set_param(process_rid, "_background_mode", terrain.material.world_background)
			# 传递地形顶点参数
			RenderingServer.material_set_param(process_rid, "_vertex_spacing", terrain.vertex_spacing)
			RenderingServer.material_set_param(process_rid, "_vertex_density", 1.0 / terrain.vertex_spacing)
			# 传递地形区域参数
			RenderingServer.material_set_param(process_rid, "_region_size", terrain.region_size)
			RenderingServer.material_set_param(process_rid, "_region_texel_size", 1.0 / terrain.region_size)
			RenderingServer.material_set_param(process_rid, "_region_map_size", 32)
			# 传递地形各类贴图RID
			RenderingServer.material_set_param(process_rid, "_region_map", terrain.data.get_region_map())
			RenderingServer.material_set_param(process_rid, "_region_locations", terrain.data.get_region_locations())
			RenderingServer.material_set_param(process_rid, "_height_maps", terrain.data.get_height_maps_rid())
			RenderingServer.material_set_param(process_rid, "_control_maps", terrain.data.get_control_maps_rid())
			RenderingServer.material_set_param(process_rid, "_color_maps", terrain.data.get_color_maps_rid())
			# 传递粒子自身参数
			RenderingServer.material_set_param(process_rid, "instance_spacing", instance_spacing)
			RenderingServer.material_set_param(process_rid, "instance_rows", rows)
			RenderingServer.material_set_param(process_rid, "max_dist", min_draw_distance)
