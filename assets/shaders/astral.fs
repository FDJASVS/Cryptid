#if defined(VERTEX) || __VERSION__ > 100 || defined(GL_FRAGMENT_PRECISION_HIGH)
	#define MY_HIGHP_OR_MEDIUMP highp
#else
	#define MY_HIGHP_OR_MEDIUMP mediump
#endif

extern MY_HIGHP_OR_MEDIUMP vec2 astral;
extern MY_HIGHP_OR_MEDIUMP number dissolve;
extern MY_HIGHP_OR_MEDIUMP number time;
extern MY_HIGHP_OR_MEDIUMP vec4 texture_details;
extern MY_HIGHP_OR_MEDIUMP vec2 image_details;
extern bool shadow;
extern MY_HIGHP_OR_MEDIUMP vec4 burn_colour_1;
extern MY_HIGHP_OR_MEDIUMP vec4 burn_colour_2;
extern MY_HIGHP_OR_MEDIUMP vec2 mouse_screen_pos;
extern MY_HIGHP_OR_MEDIUMP float hovering;
extern MY_HIGHP_OR_MEDIUMP float screen_scale;

vec4 dissolve_mask(vec4 tex, vec2 texture_coords, vec2 uv)
{
    if (dissolve < 0.001) {
        return vec4(shadow ? vec3(0.,0.,0.) : tex.xyz, shadow ? tex.a*0.3: tex.a);
    }

    float adjusted_dissolve = (dissolve*dissolve*(3.-2.*dissolve))*1.02 - 0.01; //Adjusting 0.0-1.0 to fall to -0.1 - 1.1 scale so the mask does not pause at extreme values

	float t = time * 10.0 + 2003.;
	vec2 floored_uv = (floor((uv*texture_details.ba)))/max(texture_details.b, texture_details.a);
    vec2 uv_scaled_centered = (floored_uv - 0.5) * 2.3 * max(texture_details.b, texture_details.a);

	vec2 field_part1 = uv_scaled_centered + 50.*vec2(sin(-t / 143.6340), cos(-t / 99.4324));
	vec2 field_part2 = uv_scaled_centered + 50.*vec2(cos( t / 53.1532),  cos( t / 61.4532));
	vec2 field_part3 = uv_scaled_centered + 50.*vec2(sin(-t / 87.53218), sin(-t / 49.0000));

    float field = (1.+ (
        cos(length(field_part1) / 19.483) + sin(length(field_part2) / 33.155) * cos(field_part2.y / 15.73) +
        cos(length(field_part3) / 27.193) * sin(field_part3.x / 21.92) ))/2.;
    vec2 borders = vec2(0.2, 0.8);

    float res = (.5 + .5* cos( (adjusted_dissolve) / 82.612 + ( field + -.5 ) *3.14))
    - (floored_uv.x > borders.y ? (floored_uv.x - borders.y)*(5. + 5.*dissolve) : 0.)*(dissolve)
    - (floored_uv.y > borders.y ? (floored_uv.y - borders.y)*(5. + 5.*dissolve) : 0.)*(dissolve)
    - (floored_uv.x < borders.x ? (borders.x - floored_uv.x)*(5. + 5.*dissolve) : 0.)*(dissolve)
    - (floored_uv.y < borders.x ? (borders.x - floored_uv.y)*(5. + 5.*dissolve) : 0.)*(dissolve);

    if (tex.a > 0.01 && burn_colour_1.a > 0.01 && !shadow && res < adjusted_dissolve + 0.8*(0.5-abs(adjusted_dissolve-0.5)) && res > adjusted_dissolve) {
        if (!shadow && res < adjusted_dissolve + 0.5*(0.5-abs(adjusted_dissolve-0.5)) && res > adjusted_dissolve) {
            tex.rgba = burn_colour_1.rgba;
        } else if (burn_colour_2.a > 0.01) {
            tex.rgba = burn_colour_2.rgba;
        }
    }

    return vec4(shadow ? vec3(0.,0.,0.) : tex.xyz, res > adjusted_dissolve ? (shadow ? tex.a*0.3: tex.a) : .0);
}

number hue(number s, number t, number h)
{
	number hs = mod(h, 1.)*6.;
	if (hs < 1.) return (t-s) * hs + s;
	if (hs < 3.) return t;
	if (hs < 4.) return (t-s) * (4.-hs) + s;
	return s;
}

vec4 RGB(vec4 c)
{
	if (c.y < 0.0001)
		return vec4(vec3(c.z), c.a);

	number t = (c.z < .5) ? c.y*c.z + c.z : -c.y*c.z + (c.y+c.z);
	number s = 2.0 * c.z - t;
	return vec4(hue(s,t,c.x + 1./3.), hue(s,t,c.x), hue(s,t,c.x - 1./3.), c.w);
}

vec4 HSL(vec4 c)
{
	number low = min(c.r, min(c.g, c.b));
	number high = max(c.r, max(c.g, c.b));
	number delta = high - low;
	number sum = high+low;

	vec4 hsl = vec4(.0, .0, .5 * sum, c.a);
	if (delta == .0)
		return hsl;

	hsl.y = (hsl.z < .5) ? delta / sum : delta / (2.0 - sum);

	if (high == c.r)
		hsl.x = (c.g - c.b) / delta;
	else if (high == c.g)
		hsl.x = (c.b - c.r) / delta + 2.0;
	else
		hsl.x = (c.r - c.g) / delta + 4.0;

	hsl.x = mod(hsl.x / 6., 1.);
	return hsl;
}

vec4 RGBtoHSV(vec4 rgb)
{
    vec4 hsv;
    float minVal = min(min(rgb.r, rgb.g), rgb.b);
    float maxVal = max(max(rgb.r, rgb.g), rgb.b);
    float delta = maxVal - minVal;

    // Value
    hsv.z = maxVal;

    // Saturation
    if (maxVal != 0.0)
        hsv.y = delta / maxVal;
    else {
        // r = g = b = 0, s = 0, v is undefined
        hsv.y = 0.0;
        hsv.x = -1.0;
        return hsv;
    }

    // Hue
    if (rgb.r == maxVal)
        hsv.x = (rgb.g - rgb.b) / delta;      // between yellow & magenta
    else if (rgb.g == maxVal)
        hsv.x = 2.0 + (rgb.b - rgb.r) / delta;  // between cyan & yellow
    else
        hsv.x = 4.0 + (rgb.r - rgb.g) / delta;  // between magenta & cyan

    hsv.x = hsv.x * (1.0 / 6.0);
    if (hsv.x < 0.0)
        hsv.x += 1.0;

    // Alpha
    hsv.w = rgb.a;

    return hsv;
}

vec4 HSVtoRGB(vec4 hsv) {
    vec4 rgb;

    float h = hsv.x * 6.0;
    float c = hsv.z * hsv.y;
    float x = c * (1.0 - abs(mod(h, 2.0) - 1.0));
    float m = hsv.z - c;

    if (h < 1.0) {
        rgb = vec4(c, x, 0.0, hsv.a);
    } else if (h < 2.0) {
        rgb = vec4(x, c, 0.0, hsv.a);
    } else if (h < 3.0) {
        rgb = vec4(0.0, c, x, hsv.a);
    } else if (h < 4.0) {
        rgb = vec4(0.0, x, c, hsv.a);
    } else if (h < 5.0) {
        rgb = vec4(x, 0.0, c, hsv.a);
    } else {
        rgb = vec4(c, 0.0, x, hsv.a);
    }

    rgb.rgb += m;

    return rgb;
}

float bitxor(float val1, float val2)
{
	float outp = 0.;
	for(float i = 1.; i < 9.; i++) outp += floor(mod(mod(floor(val1*pow(2.,-i)),pow(2.,i))+mod(floor(val2*pow(2.,-i)),pow(2.,i)),2.))*pow(2.,i);
	return outp/256.;
}

float mod2(float val1, float mod1)
{
    val1 /= mod1;
    val1 -= floor(val1);
    return(mod1 * val1);
}

#define PI 3.14159265358979323846

float rand(vec2 c){
	return fract(sin(dot(c.xy ,vec2(12.9898,78.233))) * 43758.5453);
}

float noise(vec2 p, float freq ){
	float unit = 1./freq;
	vec2 ij = floor(p/unit);
	vec2 xy = mod(p,unit)/unit;
	//xy = 3.*xy*xy-2.*xy*xy*xy;
	xy = .5*(1.-cos(PI*xy));
	float a = rand((ij+vec2(0.,0.)));
	float b = rand((ij+vec2(1.,0.)));
	float c = rand((ij+vec2(0.,1.)));
	float d = rand((ij+vec2(1.,1.)));
	float x1 = mix(a, b, xy.x);
	float x2 = mix(c, d, xy.x);
	return mix(x1, x2, xy.y);
}

float pNoise(vec2 p, int res){
	float persistance = .5;
	float n = 0.;
	float normK = 0.;
	float f = 4.;
	float amp = 1.;
	int iCount = 0;
	for (int i = 0; i<50; i++){
		n+=amp*noise(p, f);
		f*=2.;
		normK+=amp;
		amp*=persistance;
		if (iCount == res) break;
		iCount++;
	}
	float nf = n/normK;
	return nf*nf*nf*nf;
}

vec4 effect( vec4 colour, Image texture, vec2 texture_coords, vec2 screen_coords )
{
    vec4 tex = Texel(texture, texture_coords);
	vec2 uv = (((texture_coords)*(image_details)) - texture_details.xy*texture_details.ba)/texture_details.ba;

	// Dummy, doesn't do anything but at least it makes the shader useable
    if (uv.x > uv.x * 2.){
        uv = astral;
    }

    float mod = astral.r * 1.0;

	number low = min(tex.r, min(tex.g, tex.b));
    number high = max(tex.r, max(tex.g, tex.b));
	number delta = high - low;

	//vec4 hsl = HSL(vec4(tex.r, tex.g, tex.b, tex.a));

	float t = astral.y*2.221 + time;
	vec2 floored_uv = (floor((uv*texture_details.ba)))/texture_details.ba;
    vec2 uv_scaled_centered = (floored_uv - 0.5) * 50.;

	vec2 field_part1 = uv_scaled_centered + 50.*vec2(sin(-t / 143.6340), cos(-t / 99.4324));
	vec2 field_part2 = uv_scaled_centered + 50.*vec2(cos( t / 53.1532),  cos( t / 61.4532));
	vec2 field_part3 = uv_scaled_centered + 50.*vec2(sin(-t / 87.53218), sin(-t / 49.0000));

    float field = (1.+ (
        cos(length(field_part1) / 19.483) + sin(length(field_part2) / 33.155) * cos(field_part2.y / 15.73) +
        cos(length(field_part3) / 27.193) * sin(field_part3.x / 21.92) ))/2.;


    vec4 pixel = Texel(texture, texture_coords);

    float cx = uv_scaled_centered.x * 1.;
    float cy = uv_scaled_centered.y * 1.;



    vec4 hsl = HSL(vec4(tex.r, tex.g, tex.b, tex.a));


    // |y| = 50, |x| = 50

    tex.rgb = RGB(hsl).rgb;

    vec2 mouse_offset = (screen_coords.xy - mouse_screen_pos.xy)/screen_scale;

    float scaled_uvy = (uv.y +mouse_offset.y -0.5)*5.*1.338;
    float scaled_uvx = (uv.x +mouse_offset.x -0.5)*5.;
    float norm_uv = sqrt(scaled_uvx*scaled_uvx + scaled_uvy*scaled_uvy);

    pixel = vec4(pixel.rgb * 0.0 + tex.rgb * tex.a, pixel.a);

    float res = (.5 + .5* cos( (astral.x) * 2.612 + ( field + -.5 ) *3.14));
    vec4 textp = RGB(hsl);
    tex.rgb = textp.rgb;

	float stars = ((pNoise(uv*10. + t/15.0, 10)*pNoise(uv*10. + t/15.0, 10)+1.5)/1.+0.15 + ((pNoise(uv*12. + t/15.0, 10)+1.2)/1.+0.3))/2.2+0.05 + 0.007*norm_uv * 1.1;

	float clusters = (pNoise(uv*10.0-t/15.0, 10)+1.5)/1.5-0.25 + 0.007*norm_uv;

	float super_clusters = (pNoise(uv/15.0, 10)+0.1)/2.+0.3 - 0.008*norm_uv;

    clusters *= clusters * clusters * clusters * 0.4;
    stars *= stars * stars;
    super_clusters *= super_clusters * super_clusters;

	colour.rgb = vec3(0.6, 0.45, 1.) * (((clusters + stars + super_clusters)+0.1)) * 0.285;

	return dissolve_mask(tex*colour, texture_coords, uv);
}

#ifdef VERTEX
vec4 position( mat4 transform_projection, vec4 vertex_position )
{
    if (hovering <= 0.){
        return transform_projection * vertex_position;
    }
    float mid_dist = length(vertex_position.xy - 0.5*love_ScreenSize.xy)/length(love_ScreenSize.xy);
    vec2 mouse_offset = (vertex_position.xy - mouse_screen_pos.xy)/screen_scale;
    float scale = 0.2*(-0.03 - 0.3*max(0., 0.3-mid_dist))
                *hovering*(length(mouse_offset)*length(mouse_offset))/(2. -mid_dist);

    return transform_projection * vertex_position + vec4(0.,0.,0.,scale);
}
#endif