shader_type canvas_item;
render_mode blend_add;

//颜色
uniform float Saturation = 0.2f;
uniform vec4 spark_color : source_color = vec4(0.5, 0.3, 0.1, 1.0);

uniform sampler2D noise_tex;
uniform float projectile_speed : hint_range(0.0, 1.0) = 1.0;
uniform	float turn_amount : hint_range(0.0, 1.0) = 0.0;
uniform float visible = 1.0;
uniform float explosion_progress  : hint_range(0.0, 1.0) = 1.0;
float lineNoise(float phase)//phase的输入应当是正负PI
{
	//
	float x = abs(phase/(PI))  ;
	x = mod(x - 0.5 *TIME,1.0);
	float period = 30.0;
	float y = mod(TIME,period)/period;
	float noise =texture(noise_tex, vec2(x,y)).r;
	float noise2 = texture(noise_tex, vec2(mod(x + 0.5,1.0),y)).r;
	return mix(noise,noise2,0.5);
	//return noise2;
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

float decay(float d, float f1, float f2,float f3)
{
	float result = pow(1.0 / (d * f1 + f2), f3);
	result -= pow(1.0 / (1.0 * f1 + f2), f3);
	return result;
}
void vertex()
{
	VERTEX *= (1.0 -24.0*pow(explosion_progress - 0.5,2) +6.0);
	float stretch_coeff = projectile_speed * mix(1.0,0.0,turn_amount);
	VERTEX.x += 0.15*stretch_coeff;
}
void fragment()
{
    vec2 uv = (UV - 0.5) * 2.0;
    float r = length(uv);//0~1
    float angle = atan(uv.y, uv.x);
	float angle_diff = abs(angle - PI/4.0);
	float noise_scale = projectile_speed * 0.03*pow(angle_diff/PI,0.5)  + 0.08;
	float stretch_coeff = projectile_speed * mix(1.0,0.1,turn_amount);
	
	float d = r + noise_scale * lineNoise( angle) + 0.8*(1.0 -abs(angle)/PI)*stretch_coeff + 3.0* explosion_progress;
	float decay_f1 = 9.0;
	float decay_f2 =0.02;
	decay_f1 -= stretch_coeff* 8.0* abs(angle)/PI + 5.0*(1.0 - stretch_coeff) ;
	float rays = decay(d,decay_f1,decay_f2,3.5);
	rays = max(rays,0);
	
	float sector = PI / 2.0; 
	float a = mod(angle +PI/4.0 , sector) - sector * 0.5;
    vec2 p = vec2(cos(a), sin(a)) * r;
	float spark_d = pow(abs(p.x),0.4) + 0.8* pow(abs(p.y),0.4) ;
	float spark_size =  -pow(explosion_progress - 0.5,2) +0.25;
	spark_d = max(0,spark_d - spark_size);
    float spark_rays =explosion_progress* decay( spark_d,4.0,decay_f2,2.5);
	
	rays += spark_rays;
	vec3 color = spark_color.rgb *rays;
	
	color *= visible;
    COLOR = vec4(color, 1.0);
}