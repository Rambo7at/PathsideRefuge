shader_type spatial;
render_mode cull_disabled;

uniform sampler2D grass_texture : source_color, filter_linear;
uniform float wind_strength : hint_range(0.0, 0.5) = 0.15;  // 摇摆幅度
uniform float wind_speed : hint_range(0.0, 5.0) = 2.0;      // 摇摆速度

void vertex()
{
    float h = VERTEX.y;
    float phase = NODE_POSITION_WORLD.x * 0.7 + NODE_POSITION_WORLD.z * 0.3;
    float sway = sin(TIME * wind_speed + phase) * wind_strength;
    VERTEX.x += sway * h;
    VERTEX.z += sway * h * 0.5;
}

void fragment()
{
    vec4 tex = texture(grass_texture, UV);
    float a = tex.a;

    // 强裁剪：阈值提高，alpha 0.1~0.4 的半透明暗像素全部裁掉
    ALPHA_SCISSOR_THRESHOLD = 0.9;

    ALBEDO = tex.rgb;
    ALPHA = a;
}
