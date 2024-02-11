#include "il2cpp-config.h"

#if IL2CPP_TINY

// When the debugger is enabled, we use the big libil2cpp runtime code
// with the tiny profile. We need to build this icall for big libil2cpp,
// so direcrtly include the .cpp file to avboid code duplication.
    #include "../libil2cpptiny/icalls/mscorlib/System/Console.cpp"

#endif
