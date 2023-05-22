#pragma once
#ifndef DEFAULT_PLATFORM_PREFIX_CONFIGURE_H
#define DEFAULT_PLATFORM_PREFIX_CONFIGURE_H

// This file is only here to fall-back the #include in
// PrefixConfigure.h, on platforms which do not provide it.

#ifdef DEBUG
# undef DEBUG
#endif
#ifdef DEBUGMODE
# undef DEBUGMODE
#endif
#ifdef UNITY_RELEASE
# undef UNITY_RELEASE
#endif

// configure Unity
// UNITY_RELEASE is true for all non-debug builds
// e.g. everything built by TeamCity
#ifdef _DEBUG
    #define DEBUGMODE 1
    #define DEBUG 1
    #define UNITY_RELEASE 0
#else
    #define DEBUGMODE 0
    #define DEBUG 0
    #define UNITY_RELEASE 1
#endif

#endif //DEFAULT_PLATFORM_PREFIX_CONFIGURE_H
