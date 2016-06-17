// COPYRIGHT (C) UNKNOWN6656, 2016

__kernel void invert(__global char* source, __global char* dest, __global int* krnl, __global int* width)
{
	int id = get_global_id(0);

	dest[id] = 255 - source[id];
}

__kernel void applymatrix(__global char* source, __global char* dest, __global char* krnl, __global int* width, __global int* pixelsize)
{
	int psz = pixelsize[0];
	int id = get_global_id(0);
	int offs = id % psz;
	int pos = id - offs;
	float val = 0;

	for (int i = 0; i < psz; i++)
		val += (float)krnl[psz * offs + i] / (float)255.0 * (float)source[pos + i];

	/*if (val <= 0)
		dest[id] = 0;
	else if (val >= 255)
		dest[id] = 255;
	else*/
	dest[id] = (char)val;
}

//
// | a b c |   | x |   | ax+by+cz |
// | d e f | x | y | = | dx+ey+fz |
// | g h i |   | z |   | gx+hy+iz |
//