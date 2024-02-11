#pragma once

// Off by default, can be enabled in platform specific configuration
#ifndef UNITY_ATOMIC_USE_CLANG_ATOMICS
#   define UNITY_ATOMIC_USE_CLANG_ATOMICS 0
#endif
#ifndef UNITY_ATOMIC_USE_GCC_ATOMICS
#   define UNITY_ATOMIC_USE_GCC_ATOMICS 0
#endif

#if UNITY_ATOMIC_USE_GCC_ATOMICS || UNITY_ATOMIC_USE_CLANG_ATOMICS

#include <stdint.h>

#   if __SIZEOF_POINTER__ == 8
typedef int64_t non_atomic_word;
typedef __int128 non_atomic_word2;
#       define UNITY_ATOMIC_INT_OVERLOAD
#   elif __SIZEOF_POINTER__ == 4
typedef int32_t non_atomic_word;
typedef int64_t non_atomic_word2;
#   else
#       error unsupported __SIZEOF_POINTER__
#   endif

typedef non_atomic_word atomic_word;

// that might look weird as we have non_atomic_word2 member that should have proper alignment
// but on arm7 (32bits) on older ios, we sometimes saw unaligned acess to atomic_word2
// it seems that adding explicit align here helps
union alignas(2 * __SIZEOF_POINTER__)atomic_word2
{
    non_atomic_word2 v;
    struct
    {
        atomic_word lo;
        atomic_word hi;
    };
};

#if defined(__arm__) || defined(__arm64__) || defined(__aarch64__)
// on arm/arm64 we have custom implementation for atomic queue (so no DCAS)
#else
    #define ATOMIC_HAS_DCAS
#endif

#elif defined(__x86_64__) || defined(_M_X64)

#   include <emmintrin.h>

/// atomic_word must be 8 bytes aligned if you want to use it with atomic_* ops.
#   if defined(_MSC_VER)
typedef __int64 atomic_word;
#   else
typedef long long atomic_word;
#   endif

/// atomic_word2 must be 16 bytes aligned if you want to use it with atomic_* ops.
union atomic_word2
{
    __m128i         v;
    struct
    {
        atomic_word lo, hi;
    };
};
    #define ATOMIC_HAS_DCAS
    #define UNITY_ATOMIC_INT_OVERLOAD

#elif defined(__x86__) || defined(__i386__) || defined(_M_IX86)

/// atomic_word must be 4 bytes aligned if you want to use it with atomic_* ops.
typedef int atomic_word;

/// atomic_word2 must be 8 bytes aligned if you want to use it with atomic_* ops.
union atomic_word2
{
#   if defined(_MSC_VER)
    __int64 v;
#   else
    long long v;
#   endif
#   if !defined(__SSE2__)
    double d;
#   endif
    struct
    {
        atomic_word lo, hi;
    };
};
    #define ATOMIC_HAS_DCAS

#elif defined(_M_ARM64) || (defined(__arm64__) || defined(__aarch64__)) && (defined(__clang__) || defined(__GNUC__))

typedef long long atomic_word;
struct alignas(16) atomic_word2
{
    atomic_word lo;
    atomic_word hi;
};
#   define ATOMIC_HAS_DCAS
#   define ATOMIC_HAS_LDR
#   define UNITY_ATOMIC_INT_OVERLOAD

#elif defined(_M_ARM) || (defined(__arm__) && (defined(__ARM_ARCH_7__) || defined(__ARM_ARCH_7A__) || defined(__ARM_ARCH_7R__) || defined(__ARM_ARCH_7M__) || defined(__ARM_ARCH_7S__)) && (defined(__clang__) || defined(__GNUC__)))

typedef int atomic_word;
union atomic_word2
{
#   if defined(_MSC_VER)
    __int64 v;
#   else
    long long v;
#   endif
    struct
    {
        atomic_word lo;
        atomic_word hi;
    };
};
#   define ATOMIC_HAS_DCAS
#   if !defined(_MSC_VER)
#       define ATOMIC_HAS_LDR
#   endif


#elif PLATFORM_WEBGL

    #include <stdint.h>
typedef int32_t atomic_word;
union atomic_word2
{
    int64_t  v;
    struct
    {
        atomic_word lo;
        atomic_word hi;
    };
};

#if SUPPORT_THREADS
#   define ATOMIC_HAS_DCAS
#   define ATOMIC_HAS_LDR
#endif
#elif defined(__ppc64__) || defined(_ARCH_PPC64)

typedef long atomic_word;
#   define ATOMIC_HAS_LDR
#   define UNITY_ATOMIC_INT_OVERLOAD

#elif defined(__ppc__)

typedef int atomic_word;
#   define ATOMIC_HAS_LDR

#else

#   if defined(__LP64__)
typedef long long atomic_word;
#       define UNITY_ATOMIC_INT_OVERLOAD
#   else
typedef int atomic_word;
#   endif

struct atomic_word2
{
    atomic_word lo;
    atomic_word hi;
};

#endif

#if defined(ATOMIC_HAS_DCAS)

    #define ATOMIC_HAS_QUEUE    2

#elif (defined(__arm64__) || defined(__aarch64__)) && (defined(__clang__) || defined(__GNUC__))

    #define ATOMIC_HAS_QUEUE    1

#elif defined(__arm__) && (defined(__ARM_ARCH_7__) || defined(__ARM_ARCH_7A__) || defined(__ARM_ARCH_7R__) || defined(__ARM_ARCH_7M__) || defined(__ARM_ARCH_7S__)) && (defined(__clang__) || defined(__GNUC__))

    #define ATOMIC_HAS_QUEUE    1

#else

    #define ATOMIC_HAS_QUEUE    0

#endif
