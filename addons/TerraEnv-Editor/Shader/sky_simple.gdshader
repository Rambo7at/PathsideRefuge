shader_type sky;
render_mode use_debanding;
const float INV_TAU = 0.15915494;
const float INV_PI = 0.31830988;
const float PI4 = PI * 4.0;

// ================= 大气 =================
group_uniforms atmosphere;
uniform vec4 day_top : source_color = vec4(0.384, 0.455, 0.549, 1.0);      // 白天天空顶部色
uniform vec4 day_horizon : source_color = vec4(0.646, 0.656, 0.671, 1.0);  // 白天地平线色
uniform vec4 dusk_top : source_color = vec4(0.42, 0.28, 0.32, 1.0);        // 黄昏天空顶部色
uniform vec4 dusk_horizon : source_color = vec4(0.85, 0.55, 0.35, 1.0);    // 黄昏地平线色（橙红）
uniform vec4 night_top : source_color = vec4(0.012, 0.016, 0.04, 1.0);    // 夜晚天空顶部色（深蓝黑）
uniform vec4 night_horizon : source_color = vec4(0.05, 0.06, 0.10, 1.0);  // 夜晚地平线色
uniform vec4 ground_color : source_color = vec4(0.06, 0.06, 0.07, 1.0);   // 地平线以下地面色
uniform float horizon_offset = 0.0;                                        // 地平线垂直偏移

// ================= 太阳 =================
group_uniforms sun;
uniform float sun_disk_size = 0.022;                                       // 太阳圆盘大小
uniform vec4 sun_disk_color : source_color = vec4(1.0, 0.92, 0.72, 1.0);  // 太阳圆盘颜色
uniform float sun_disk_intensity = 10.0;                                   // 太阳圆盘亮度
uniform float sun_glow_size = 0.09;                                        // 太阳辉光范围
uniform float sun_glow_intensity = 1.5;                                    // 太阳辉光强度

// ================= 月亮 =================
group_uniforms moon;
uniform sampler2D moon_texture : source_color, repeat_disable;             // 月亮表面贴图
uniform float moon_size = 0.045;                                           // 月亮大小
uniform vec3 moon_alignment = vec3(0.0, 0.0, 0.0);                         // 月面朝向微调（度）
uniform float moon_brightness = 1.8;                                       // 月亮自发光亮度
uniform vec4 moon_tint : source_color = vec4(1.0, 0.98, 0.92, 1.0);      // 月亮颜色（暖白）

// ================= 星空 =================
group_uniforms stars;
uniform sampler2D star_texture : source_color;                             // 星空全景贴图
uniform vec3 star_alignment = vec3(0.0, 0.0, 0.0);                         // 星空对齐微调
uniform float star_intensity = 1.0;                                        // 星星亮度
uniform float star_fade = 0.25;                                            // 星星淡入阈值（太阳低于此角度星星出现）

// ================= 云 =================
group_uniforms clouds;
uniform sampler2D cumulus_texture : source_color;                          // 云噪声贴图
uniform bool cumulus_visible = true;                                       // 云显隐
uniform float cumulus_intensity = 0.6;                                     // 云亮度
uniform float cumulus_coverage = 0.55;                                     // 云覆盖率（0无云 1满天）
uniform float cumulus_thickness = 0.0243;                                  // 云厚度（体积步长）
uniform float cumulus_absorption = 2.0;                                    // 云吸光（越大越实越暗）
uniform float cumulus_noise_freq = 2.7;                                    // 云噪声频率（越大越碎）
uniform float cumulus_size = 0.5;                                          // 云大小（球壳缩放）
uniform vec2 cumulus_position = vec2(0.0);                                 // 云偏移（C# 每帧滚动=风）
uniform vec3 cumulus_partial_mie_phase = vec3(0.95756, 1.04244, 0.412);    // 云米氏散射相位参数
uniform float cumulus_mie_intensity = 1.0;                                // 云边缘散射强度
uniform vec4 clouds_night_color : source_color = vec4(0.09, 0.094, 0.129, 1.0);  // 夜晚云颜色
uniform vec4 sun_light_color : source_color = vec4(1.0);                  // 太阳光颜色（C# 每帧传，云染色）

// 旋转矩阵
mat3 rot_x(float a) {
    float c = cos(a); float s = sin(a);
    return mat3(vec3(1.0, 0.0, 0.0), vec3(0.0, c, -s), vec3(0.0, s, c));
}
mat3 rot_y(float a) {
    float c = cos(a); float s = sin(a);
    return mat3(vec3(c, 0.0, s), vec3(0.0, 1.0, 0.0), vec3(-s, 0.0, c));
}
mat3 rot_z(float a) {
    float c = cos(a); float s = sin(a);
    return mat3(vec3(c, -s, 0.0), vec3(s, c, 0.0), vec3(0.0, 0.0, 1.0));
}

// 等距柱状 UV
vec2 equirect_uv(vec3 dir) {
    float u = (atan(dir.y, dir.x) + PI) * INV_TAU;
    float v = acos(dir.z) * INV_PI;
    return vec2(u, v);
}

// 射线-球求交（画月亮圆盘）
float simple_sphere_intersect(vec3 ray_dir, vec3 sphere_pos, float radius) {
    vec3 oc = -sphere_pos;
    float b = dot(oc, ray_dir);
    float c = dot(oc, oc) - radius * radius;
    float h = b * b - c;
    float valid = step(0.0, h);
    h = sqrt(max(h, 0.0));
    return mix(-1.0, -b - h, valid);
}

// 月亮贴图采样（用圆盘法线，球面 UV）
vec3 sample_moon(vec3 dir) {
    dir = rot_x(radians(moon_alignment.x)) * rot_y(radians(moon_alignment.y)) * rot_z(radians(moon_alignment.z)) * dir;
    dir.x = -dir.x;
    dir.z = -dir.z;
    float u = 0.5 - atan(dir.z, dir.x) * INV_TAU;
    float v = 0.5 - asin(dir.y) * INV_PI;
    return texture(moon_texture, vec2(u, v)).rgb;
}

// ---------------- 云工具函数（移植自 Sky3D） ----------------

float clamp_to_unit(float value) {
    return clamp(value, 0.0, 1.0);
}

float calc_mie_phase(float mu, vec3 partial) {
    float denominator = partial.y - partial.z * mu;
    float safe_denominator = max(denominator, 0.0001); // 防止除零
    return PI4 * partial.x * pow(safe_denominator, -1.5);
}

float map_range(float value, float from_min, float from_max, float to_min, float to_max) {
    return to_min + (value - from_min) * (to_max - to_min) / (from_max - from_min);
}

// 与云层球壳求交（球心在 Y=450，半径 500）
bool ray_sphere_intersect(float r, vec3 origin, vec3 dir, out float t, out vec3 nrm) {
    origin += vec3(0.0, 450.0, 0.0);
    float a = dot(dir, dir);
    float b = 2.0 * dot(origin, dir);
    float c = dot(origin, origin) - r * r;
    float d = b * b - 4.0 * a * c;
    float valid = step(0.0, d);
    d = sqrt(max(d, 0.0));
    a *= 2.0;
    float t1 = 0.5 * (-b + d);
    float t2 = 0.5 * (-b - d);
    t = mix(t2, t1, step(t2, t1) * step(0.0, t1));
    t = mix(0.0, t, step(0.0, t) * valid);
    nrm = mix(vec3(0.0), origin + t * dir, step(0.0, t) * valid);
    return bool(step(0.0, t) * valid);
}

const int CUMULUS_STEP = 10;

float sample_cumulus_noise(vec3 p) {
    vec3 pos = vec3(p * 0.01);
    pos.z *= 256.0;
    vec2 offset = vec2(0.317, 0.123);
    vec4 uv = vec4(0.0);
    uv.xy = pos.xy + offset * floor(pos.z);
    uv.zw = uv.xy + offset;
    float x1 = textureLod(cumulus_texture, uv.xy, 0.0).r;
    float x2 = textureLod(cumulus_texture, uv.zw, 0.0).r;
    return mix(x1, x2, fract(pos.z));
}

// FBM：4 层噪声叠加 → 块状云
float calc_cumulus_fbm(vec3 p, float l) {
    float ret;
    ret = 0.51749673 * sample_cumulus_noise(p);
    p *= l;
    ret += 0.25584929 * sample_cumulus_noise(p);
    p *= l;
    ret += 0.12527603 * sample_cumulus_noise(p);
    p *= l;
    ret += 0.06255931 * sample_cumulus_noise(p);
    return ret;
}

float calc_cumulus_density(vec3 p, vec3 offset, float t) {
    vec3 pos = p * 0.0212242 - offset;
    float dens = calc_cumulus_fbm(pos, cumulus_noise_freq);
    dens += dens;
    float cov = 1.0 - cumulus_coverage;
    cov = smoothstep(0.00, (cov * 3.5) + t, dens);
    dens *= cov;
    dens = map_range(dens, 1.0 - cov, 1.0, 0.0, 1.0);
    return clamp_to_unit(dens);
}

// 积云：球壳内 ray marching（简化体积云）
vec4 render_cumulus_clouds(vec3 ro, vec3 rd, float am, vec3 sun_pos, vec3 moon_pos) {
    vec4 ret = vec4(0.0);
    vec3 wind = -vec3(cumulus_position.x, 0.0, cumulus_position.y);
    float a = 0.0;
    vec3 n; float tt;
    if (ray_sphere_intersect(500.0, ro, rd, tt, n)) {
        float march_step = float(CUMULUS_STEP) * cumulus_thickness;
        vec3 dir_step = rd / max(rd.y, 0.05) * march_step;
        vec3 pos = n * cumulus_size;
        vec2 mu = vec2(dot(sun_pos, rd), dot(moon_pos, rd));
        vec3 mph = (calc_mie_phase(mu.x, cumulus_partial_mie_phase) * sun_light_color.rgb) +
                   calc_mie_phase(mu.y, cumulus_partial_mie_phase) * am;
        vec4 t = vec4(1.0);
        t.rgb += mph.rgb * cumulus_mie_intensity;
        for (int i = 0; i < CUMULUS_STEP; i++) {
            float h = float(i) * 0.1;
            float density = calc_cumulus_density(pos, wind, h);
            float sh = clamp_to_unit(exp(-cumulus_absorption * density * march_step));
            t *= sh;
            ret += t * (exp(h) * 0.571428571) * density * march_step;
            a += (1.0 - sh) * (1.0 - a);
            pos += dir_step;
        }
        return vec4(ret.rgb * cumulus_intensity, a);
    }
    return vec4(ret.rgb * cumulus_intensity, a);
}

void sky() {
    vec3 dir = EYEDIR;
    vec3 sun_pos = LIGHT0_DIRECTION;
    vec3 moon_pos = LIGHT1_DIRECTION;

    float sun_y = sun_pos.y;

    // 白天 / 黄昏 / 夜晚因子
    float day = clamp(sun_y, 0.0, 1.0);
    float dusk = clamp(1.0 - abs(sun_y) / 0.25, 0.0, 1.0);
    float night = clamp(-sun_y + star_fade, 0.0, 1.0);

    // ---- 大气 ----
    vec3 top = mix(night_top.rgb, day_top.rgb, day);
    top = mix(top, dusk_top.rgb, dusk);
    vec3 hor = mix(night_horizon.rgb, day_horizon.rgb, day);
    hor = mix(hor, dusk_horizon.rgb, dusk);

    float h = dir.y - horizon_offset;
    vec3 col = mix(hor, top, smoothstep(-0.02, 0.15, h));
    col = mix(col, ground_color.rgb, smoothstep(0.02, -0.15, h));

    // ---- 太阳：圆盘 + 辉光 ----
    float sun_dist = length(dir - sun_pos);
    float disk = 1.0 - smoothstep(sun_disk_size * 0.8, sun_disk_size, sun_dist);
    float glow = pow(1.0 - smoothstep(0.0, sun_glow_size, sun_dist), 3.0);
    col += sun_disk_color.rgb * disk * sun_disk_intensity;
    col += sun_disk_color.rgb * glow * sun_glow_intensity * day;

    // ---- 月亮：圆盘 + 贴图（自身发光，周围保持黑）----
    float moon_intersect = simple_sphere_intersect(dir, moon_pos, moon_size);
    float moon_mask = moon_intersect > (-1.0 + sqrt(moon_size)) ? 1.0 : 0.0;
    float moon_vis = smoothstep(0.05, -0.05, sun_y);   // 太阳完全落下后月亮全亮
    if (moon_mask > 0.0) {
        vec3 moon_normal = normalize(dir * moon_intersect - moon_pos);  // 圆盘表面法线
        vec3 mcol = sample_moon(moon_normal);
        col += mcol * moon_tint.rgb * moon_brightness * moon_vis;
    }
    // 极小月晕（仅让边缘柔和，不泛白）
    float moon_dist = length(dir - moon_pos);
    float mglow = pow(1.0 - smoothstep(0.0, moon_size * 2.5, moon_dist), 3.0);
    col += moon_tint.rgb * mglow * moon_vis * 0.12;

    // ---- 星空：夜晚淡入，月亮周围压暗 ----
    vec3 sd = rot_x(radians(star_alignment.x)) * rot_y(radians(star_alignment.y)) * rot_z(radians(star_alignment.z)) * dir;
    vec3 star_col = textureLod(star_texture, equirect_uv(normalize(sd)), 0.0).rgb;
    col += star_col * star_intensity * night * (1.0 - moon_mask * 0.85);

    // ---- 云：简化体积云（ray marching）----
    if (cumulus_visible) {
        float am = clamp(-sun_y + 0.30, 0.0, 1.0);   // 夜晚因子（月光贡献）
        vec4 cloud = render_cumulus_clouds(vec3(0.0), dir, am, sun_pos, moon_pos);
        cloud.a = clamp_to_unit(cloud.a);
        vec3 cloud_tint = mix(clouds_night_color.rgb, sun_light_color.rgb, clamp(sun_y + 0.35, 0.0, 1.0));
        cloud.rgb *= cloud_tint * cumulus_intensity;
        cloud.a *= clamp_to_unit((dir.y + 0.01) * 50.0);   // 地平线附近淡出
        col = mix(col, cloud.rgb, cloud.a);
    }

    COLOR = col;
}
