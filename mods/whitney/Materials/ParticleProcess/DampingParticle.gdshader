shader_type particles;

uniform float INIT_SPEED = 200;
uniform float MIN_SPEED = 1;
uniform float SIZE = 1;
uniform float DRAG = 2;
uint hash(uint x) {
	x = ((x >> uint(16)) ^ x) * uint(73244475);
	x = ((x >> uint(16)) ^ x) * uint(73244475);
	x = (x >> uint(16)) ^ x;
	return x;
}
float rand_from_seed(inout uint seed) {
	int k;
	int s = int(seed);
	if (s == 0) {
		s = 305420679;
	}
	k = s / 127773;
	s = 16807 * (s - k * 127773) - 2836 * k;
	if (s < 0) {
		s += 2147483647;
	}
	seed = uint(s);
	return float(seed % uint(65536)) / 65535.0;
}

void start() {
	uint base_number = NUMBER;
	uint alt_seed = hash(base_number + uint(1) + RANDOM_SEED);
	float speed = INIT_SPEED * (0.75+0.5 * (rand_from_seed(alt_seed))) ;
	float rot = TAU * rand_from_seed(alt_seed);
	VELOCITY =speed * vec3(sin(rot),cos(rot),0.0);
	TRANSFORM[3] = vec4(0.0);
	
	//粒子存活时间
	CUSTOM.x = 0.0;
}

void process()
{
	if (RESTART)
	{
		CUSTOM.x = 0.0;
	}
		
	float speed = length(VELOCITY);
	float progress = CUSTOM.x / LIFETIME;

	float scale = clamp(5.0*( progress - progress*progress),0.0,1.0);
	TRANSFORM[0][0] =scale;
	TRANSFORM[1][1] = scale;
	speed = mix(speed, MIN_SPEED, DELTA * DRAG);
	VELOCITY = speed * normalize(VELOCITY);
	
	CUSTOM.x += DELTA;
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
