__kernel void convolveKernel(__global char* source, __global char* dest, __global int* krnl, __global int* width)
{    
    int id = get_global_id(0);
    int wid = width[0];
    int x = id % wid;
    int y = id / wid;
    int out = krnl[4] * source[id];

    if (x > 0)
	{
		out += krnl[3] * source[id - 1];

        if (y > 0)
            out += krnl[0] * source[id - 1 - wid];
        if (y < wid - 1)
            out += krnl[6] * source[id - 1 + wid];
    }

	if (x < wid - 1)
	{
        out += krnl[5] * source[id + 1];

        if (y > 0)
            out += krnl[2] * source[id + 1 - wid];
        if (y < wid - 1)
            out += krnl[8] * source[id + 1 + wid];
    }

    if (y > 0)
        out += krnl[1] * source[id - wid];
    if (y < wid - 1)
        out += krnl[7] * source[id + wid];

    dest[id] = out / 4;
}
