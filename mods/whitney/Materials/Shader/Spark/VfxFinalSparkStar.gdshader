shader_type canvas_item;
render_mode blend_add;
//render_mode blend_sub;

uniform float glow_factor = 4.5;

uniform float progress: hint_range(0.0, 2.0) = 0.0f;
uniform float size = 1.0;

uniform sampler2D noise_tex;
void vertex() {
	//VERTEX *= (0.5+0.5* size);
}
//c的x,y,z取值范围均为0~1
vec3 hsv2rgb(vec3 c){
    vec3 rgb = clamp(abs(mod(c.x*6.0 + vec3(0,4,2),6.0)-3.0)-1.0,0.0,1.0);
    return c.z * mix(vec3(1.0), rgb, c.y);
}
//归一化亮度
vec3 norm_col(vec3 c){
	float lum = dot(c, vec3(0.2126, 0.7152, 0.0722));
	c /= (lum + 1e-4);
	return c;
}

vec3 rainbow_uniform(float t) {
    vec3 rgb;
    rgb.r = sin(t * 6.28318 + 0.0);
    rgb.g = sin(t * 6.28318 + 2.09439);
    rgb.b = sin(t * 6.28318 + 4.18879);
    rgb = (rgb + 1.0) * 0.5;
    float saturation = 0.8;
    float value = 1.0;
    return value * (rgb * saturation + vec3(1.0 - saturation));
}

float lineNoise(float start,float speed)//phase的输入应当是正负PI
{
	float x = fract(start);
	//x = mod(x - 0.5 *TIME,1.0);
	float y =fract(TIME * speed);
	float noise =texture(noise_tex, vec2(x,y)).r;
	float noise2 = texture(noise_tex, vec2(mod(x + 0.5,1.0),y)).r;
	return mix(noise,noise2,0.5);
}
vec3 ring(float ring_progress,float angle,float dis,float max_radius,float brightness)
{
	vec3 ringColor = norm_col(hsv2rgb(vec3(-TIME + angle/(PI * 2.0),0.7,1)));
	float ring_radius = max_radius * ring_progress;
	float ring_sharp =2.0 - 0.1* ring_progress;
	float ring_brightness = brightness * pow(lineNoise(angle/TAU + 0.4,0.1),6) ;
	ring_brightness *= 4.0* step(0.0001,ring_progress);
	return ringColor *  exp(-abs((dis-ring_radius)*ring_sharp) * 50.0)*ring_brightness;
}
void fragment()
{
	float pulse_period = 1.5;
	float pulse_amplitude = 0.08;
    float pulse = 1.0 + pulse_amplitude * sin(TIME * 6.28318 / pulse_period);

    vec2 uv = (UV - 0.5) * 2.0;
    float r = length(uv);//0~1

	float total_spin = PI*1.25;
	float spin_progress = min(total_spin * progress,total_spin) ;//期望在后半段不旋转
    float angle = atan(uv.y, uv.x) +spin_progress;
    float sector = 6.28318 / 4.0;
    float a = mod(angle + sector * 0.5, sector) - sector * 0.5;
    // 旋转到标准方向
    vec2 p = vec2(cos(a), sin(a)) * r;
    // 星芒参数
	float star_progress = min(1.0,progress);
	star_progress -=abs(0.5*max(0.0,progress-1.0));
	float factor3 = 14.0-7.0*star_progress*star_progress*star_progress*star_progress;
	float factor2 = 0.1+ 0.25 * pow(abs(star_progress-0.5)*2.0,0.4) + 0.15 * star_progress;
	float factor = -0.8+  1.8* star_progress;// progress*0.5 +  0.5;
	factor *= pow(pulse,0.3);
	factor2 *= sqrt(pulse);
	factor3 *= pulse;
	//factor= test;
    float d = (pow(abs(p.x),factor2) + factor* pow(abs(p.y),factor2)) - 0.05*size +0.05;
	d = max(0,d);
	float glow = glow_factor;
    float rays = pow(1.0 / (d * factor3 + 0.02), glow);

	vec3 hsv = vec3(TIME + angle/(PI * 2.0),0.5,1);
	vec3 colorRGB = norm_col(hsv2rgb(hsv));
	vec3 color = colorRGB * rays;

	float ring_progress = 2.5*max(progress - 0.6,0.0);
	float ring_brightness = min(1.0,2.0-progress);
	color +=  ring(ring_progress,angle,r,0.06,ring_brightness);
	float ring_progress2 =  min(1.0,ring_progress*1.1);
	float ring2_brightness = clamp(1.0-ring_progress2,0.0,0.9) ;
	ring2_brightness = pow(ring2_brightness , 0.5);
	color +=  ring(ring_progress2,-angle + 0.5,r,0.2,ring2_brightness);
	
    COLOR = vec4(color, 1.0);
}
