shader_type spatial;
render_mode cull_back;

uniform sampler2D cover_noise;
uniform float roughness = 1.0;
uniform float metallic = 0.03;
uniform float specular = 0.5;

uniform float cover_noise_scale = 8.0;
uniform float slope_cover_start = 25.0;
uniform float slope_cover_end = 50.0;
uniform float slope_rock_start = 40.0;
uniform float slope_rock_end = 60.0;

group_uniforms dirtMat;
uniform vec3 Color_dirtMat : source_color = vec3(1.0, 1.0, 1.0);
uniform sampler2D Text_dirt : source_color;
uniform sampler2D Normal_dirt;
uniform float dirt_uv_scale = 1;

group_uniforms StoneMat;
uniform vec3 Color_StoneMat : source_color = vec3(1.0, 1.0, 1.0);
uniform sampler2D Text_Stone : source_color;
uniform sampler2D Normal_Stone;
uniform float stone_uv_scale = 1;

group_uniforms GrasslandMat;
uniform vec3 Color_Grassland : source_color = vec3(1.0, 1.0, 1.0);
uniform sampler2D Text_Grassland : source_color;
uniform sampler2D Normal_Grassland;
uniform float grass_uv_scale = 1;
uniform float grass_opacity = 0.8;

group_uniforms Splatmap;
uniform sampler2D splat_map : source_color, repeat_disable;

void fragment()
{
    float grass_weight = texture(splat_map, UV).r;

    // NORMAL 是视图空间，需要转世界空间
    vec3 normal_world = (INV_VIEW_MATRIX * vec4(NORMAL, 0.0)).xyz;
    float slope = degrees(acos(clamp(normal_world.y, 0.0, 1.0)));

    // ===== 底色层：泥土 → 石头 =====
    float rock_factor = smoothstep(slope_rock_start, slope_rock_end, slope);
    vec3 dirt_color = Color_dirtMat * texture(Text_dirt, UV * dirt_uv_scale).rgb;
    vec3 stone_color = Color_StoneMat * texture(Text_Stone, UV * stone_uv_scale).rgb;
    vec3 base_color = mix(dirt_color, stone_color, rock_factor);

    vec3 n_dirt = texture(Normal_dirt, UV * dirt_uv_scale).rgb * 2.0 - 1.0;
    vec3 n_stone = texture(Normal_Stone, UV * stone_uv_scale).rgb * 2.0 - 1.0;
    vec3 base_normal = normalize(mix(n_dirt, n_stone, rock_factor));

    // ===== 自然层：草原 =====
    float slope_factor = smoothstep(slope_cover_start, slope_cover_end, slope);
    float noise = texture(cover_noise, UV * cover_noise_scale).r;
    float adjusted = slope_factor + (noise - 0.5) * 0.4;
    float cover = (1.0 - smoothstep(0.0, 1.0, adjusted)) * grass_weight;

    vec3 grass_color = Color_Grassland * texture(Text_Grassland, UV * grass_uv_scale).rgb;
    vec3 n_grass = texture(Normal_Grassland, UV * grass_uv_scale).rgb * 2.0 - 1.0;
    vec3 final_normal = normalize(mix(base_normal, n_grass, cover));

    float grass_alpha = cover * grass_opacity;
    ALBEDO = mix(base_color, grass_color, grass_alpha);
    NORMAL_MAP = final_normal * 0.5 + 0.5;
    ROUGHNESS = roughness;
    METALLIC = metallic;
    SPECULAR = specular;
}
