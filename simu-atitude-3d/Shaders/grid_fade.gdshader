shader_type spatial;
render_mode unshaded;

uniform vec3 grid_color = vec3(0.5, 0.5, 0.5);
uniform float grid_step = 1.0;
uniform float line_thickness = 0.05;
uniform float fade_distance = 50.0;

varying vec3 varying_vertex;

void vertex() {
	varying_vertex = VERTEX;
}

void fragment() {
    // Posição local do vértice para desenhar a linha
    vec2 pos = varying_vertex.xz;
    vec2 grid = fract(pos / grid_step);
    
    // Calcula distância pro centro (0,0) para aplicar fade de visibilidade
    float dist = length(pos);
    float alpha_fade = 1.0 - clamp(dist / fade_distance, 0.0, 1.0);
    
    float lines = step(grid.x, line_thickness) + step(grid.y, line_thickness);
    
    if (lines < 1.0) {
        discard;
    }
    
    ALBEDO = grid_color;
    ALPHA = alpha_fade;
}
