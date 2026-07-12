shader_type canvas_item;
render_mode blend_add;
uniform float power : hint_range(0.0, 1.0) = 1.0;
uniform sampler2D noise_tex;
float lineNoise(float start,float speed)
{
	float x = fract(start);
	//x = mod(x - 0.5 *TIME,1.0);
	float y =fract(TIME * speed);
	float noise =texture(noise_tex, vec2(x,y)).r;
	float noise2 = texture(noise_tex, vec2(mod(x + 0.5,1.0),y)).r;
	return mix(noise,noise2,0.5);
}

vec3 hsv2rgb(vec3 c){
    vec3 rgb = clamp(abs(mod(c.x*6.0 + vec3(0,4,2),6.0)-3.0)-1.0,0.0,1.0);
    c= c.z * mix(vec3(1.0), rgb, c.y);
	float lum = dot(c, vec3(0.2126, 0.7152, 0.0722));
	c /= (lum + 1e-4);
	return c;
}

vec3 ring(float angle,float dis,float max_radius,float brightness)
{
	vec3 ringColor = hsv2rgb(vec3(-TIME + angle/(PI * 2.0),0.7,1));
	float ring_radius = max_radius ;
	float ring_sharp =1.0;
	float ring_brightness = brightness * pow(lineNoise(angle/TAU + 0.4,0.1),6) ;
	ring_brightness *= 4.0* step(0.0001,1.0);
	return ringColor *  exp(-abs((dis-ring_radius)*ring_sharp) * 50.0)*ring_brightness;
}

void vertex() {
	VERTEX *= 500.0;
}

void fragment() {
    vec2 uv = (UV - 0.5) * 2.0;
    float r = length(uv);//0~1

	float total_spin = PI*1.25;
    float angle = atan(uv.y, uv.x);

	float ring_brightness = smoothstep(0.0,1.0,power);
	vec3 color =  ring(angle,r,0.06,ring_brightness);
	COLOR = vec4(color, 1.0);
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
