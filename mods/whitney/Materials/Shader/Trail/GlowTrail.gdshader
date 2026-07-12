shader_type canvas_item;
render_mode blend_add;
uniform vec4 spark_color : source_color = vec4(0.5, 0.3, 0.1, 1.0);
uniform float visible = 1.0;

float decay(float d,float d0, float f1)
{
	//f1,f2共同决定边界半径，f3决定辉光散布
	float result = pow(1.0 / (d * f1 + 0.01), 1.5);
	result -= pow(1.0 / ((0.5+d0) * f1 + 0.01), 1.5);//确保边界处辉光归零
	return result;
}
void fragment() {
	// Called for every pixel the material is visible on.
	float dy = abs(UV.y - 0.5);
	float dx  = 1.0- UV.x;
	float d0 = 0.25;
	float f1 = 1.0;
	float f2 =0.05;
	float glow = decay(dy+d0,d0,2);
	COLOR = spark_color * glow *visible;
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
