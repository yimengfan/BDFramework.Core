#include "il2cpp-config.h"

#if IL2CPP_SUPPORT_THREADS

#include "AtomicQueue.h"
#include "ExtendedAtomicOps.h"

UNITY_PLATFORM_BEGIN_NAMESPACE;

//
//  AtomicNode
//

AtomicNode* AtomicNode::Link(AtomicNode* next)
{
#if defined(ATOMIC_HAS_QUEUE)
    AtomicNode* old = Next();
    atomic_store_explicit(&_next, (atomic_word)next, memory_order_relaxed);
    return old;
#else
    AtomicNode* old = Next();
    _next = (atomic_word)next;
    return old;
#endif
}

#if defined(ATOMIC_HAS_QUEUE)

#if !IL2CPP_TARGET_JAVASCRIPT

//
//  AtomicStack
//

AtomicStack::AtomicStack()
{
#if defined(ATOMIC_HAS_DCAS)

    atomic_word2 w;
    w.lo = w.hi = 0;
    atomic_store_explicit(&_top, w, memory_order_relaxed);

#else

    atomic_store_explicit(&_top, 0, memory_order_relaxed);

#endif
}

AtomicStack::~AtomicStack()
{
}

int AtomicStack::IsEmpty() const
{
#if defined(ATOMIC_HAS_DCAS)

    atomic_word2 top = atomic_load_explicit(&_top, memory_order_relaxed);
    return top.lo == 0;

#else

    return atomic_load_explicit(&_top, memory_order_relaxed) == 0;

#endif
}

void AtomicStack::Push(AtomicNode* node)
{
#if defined(ATOMIC_HAS_DCAS)

    atomic_word2 top = atomic_load_explicit(&_top, memory_order_relaxed);
    atomic_word2 newtop;

    newtop.lo = (atomic_word)node;
    do
    {
        atomic_store_explicit(&node->_next, top.lo, memory_order_relaxed);
        newtop.hi = top.hi + 1;
    }
    while (!atomic_compare_exchange_strong_explicit(&_top, &top, newtop, memory_order_release, memory_order_relaxed));

#elif defined(UNITY_ATOMICQUEUE_USE_CLANG_ARM_ATOMICS)

    do
    {
        node->_next = UNITY_ATOMIC_ARMV8_LDAEX(&_top);
        UNITY_ATOMIC_ARMV7_DMB_ISH
    }
    while (UNITY_ATOMIC_ARMV8_STLEX((atomic_word)node, &_top));
    UNITY_ATOMIC_ARMV7_DMB_ISH

#elif defined(__arm64__)

    AtomicNode * top;
    long success;
    __asm__ __volatile__
    (
        "0:\n\t"
        "ldxr\t%0, [%4]\n\t"
        "str    %0, [%5]\n\t"
        "stlxr\t%w3, %5, [%4]\n\t"
        "cbnz\t%w3, 0b\n\t"

        : "=&r" (top), "=m" (*node), "+m" (_top), "=&r" (success)
        : "r" (&_top), "r" (node)
        : "cc", "memory"
    );

#elif defined(__arm__)

    AtomicNode* top;
    int success;

    __asm__ __volatile__
    (
        "dmb\tish\n\t"
        "0:\n\t"
        "ldrex\t%0, [%4]\n\t"
        "str    %0, [%5]\n\t"
        "strex\t%3, %5, [%4]\n\t"
        "teq    %3, #0\n\t"
        "bne\t0b\n\t"

        : "=&r" (top), "=m" (*node), "+m" (_top), "=&r" (success)
        : "r" (&_top), "r" (node)
        : "cc", "memory"
    );

#elif defined(__ppc64__) || defined(_ARCH_PPC64)

    AtomicNode* top;
    AtomicNode* tmp;

    __asm__ __volatile__
    (
        "ld\t\t%0, 0(%4)\n\t"
        "0:\n\t"
        "mr\t\t%3, %0\n\t"
        "std    %0, 0(%5)\n\t"
        "lwsync\n\t"
        "ldarx\t%0, 0, %4\n\t"
        "cmpd\t%0, %3\n\t"
        "bne-\t0b\n\t"
        "stdcx.\t%5, 0, %4\n\t"
        "bne-\t0b\n\t"

        : "=&b" (top), "+m" (_top), "=m" (*node), "=&b" (tmp)
        : "b" (&_top), "b" (node)
        : "cr0", "memory"
    );

#elif defined(__ppc__)

    AtomicNode* top;
    AtomicNode* tmp;

    __asm__ __volatile__
    (
        "lwz\t%0, 0(%4)\n\t"
        "0:\n\t"
        "mr\t\t%3, %0\n\t"
        "stw    %0, 0(%5)\n\t"
        "lwsync\n\t"
        "lwarx\t%0, 0, %4\n\t"
        "cmpw\t%0, %3\n\t"
        "bne-\t0b\n\t"
        "stwcx.\t%5, 0, %4\n\t"
        "bne-\t0b\n\t"

        : "=&b" (top), "+m" (_top), "=m" (*node), "=&b" (tmp)
        : "b" (&_top), "b" (node)
        : "cr0", "memory"
    );

#else

#   error

#endif
}

void AtomicStack::PushAll(AtomicNode* first, AtomicNode* last)
{
#if defined(ATOMIC_HAS_DCAS)

    atomic_word2 top = atomic_load_explicit(&_top, memory_order_relaxed);
    atomic_word2 newtop;

    newtop.lo = (atomic_word)first;
    do
    {
        atomic_store_explicit(&last->_next, top.lo, memory_order_relaxed);
        newtop.hi = top.hi + 1;
    }
    while (!atomic_compare_exchange_strong_explicit(&_top, &top, newtop, memory_order_release, memory_order_relaxed));

#elif defined(UNITY_ATOMICQUEUE_USE_CLANG_ARM_ATOMICS)

    do
    {
        last->_next = UNITY_ATOMIC_ARMV8_LDAEX(&_top);
        UNITY_ATOMIC_ARMV7_DMB_ISH
    }
    while (UNITY_ATOMIC_ARMV8_STLEX((atomic_word)first, &_top));
    UNITY_ATOMIC_ARMV7_DMB_ISH

#elif defined(__arm64__)

    AtomicNode * top;
    long success;

    __asm__ __volatile__
    (
        "0:\n\t"
        "ldxr\t%0, [%4]\n\t"
        "str    %0, [%6]\n\t"
        "stlxr\t%w3, %5, [%4]\n\t"
        "cbnz\t%w3, 0b\n\t"

        : "=&r" (top), "+m" (_top), "=m" (*last), "=&r" (success)
        : "r" (&_top), "r" (first), "r" (last)
        : "cc", "memory"
    );

#elif defined(__arm__)

    AtomicNode* top;
    int success;

    __asm__ __volatile__
    (
        "dmb\tish\n\t"
        "0:\n\t"
        "ldrex\t%0, [%4]\n\t"
        "str    %0, [%6]\n\t"
        "strex\t%3, %5, [%4]\n\t"
        "teq    %3, #0\n\t"
        "bne\t0b\n\t"

        : "=&r" (top), "+m" (_top), "=m" (*last), "=&r" (success)
        : "r" (&_top), "r" (first), "r" (last)
        : "cc", "memory"
    );

#elif defined(__ppc64__) || defined(_ARCH_PPC64)

    AtomicNode* top;
    AtomicNode* tmp;

    __asm__ __volatile__
    (
        "ld\t\t%0, 0(%4)\n\t"
        "0:\n\t"
        "mr\t\t%3, %0\n\t"
        "std    %0, 0(%6)\n\t"
        "lwsync\n\t"
        "ldarx\t%0, 0, %4\n\t"
        "cmpd\t%0, %3\n\t"
        "bne-\t0b\n\t"
        "stdcx.\t%5, 0, %4\n\t"
        "bne-\t0b\n\t"

        : "=&b" (top), "+m" (_top), "=m" (*last), "=&b" (tmp)
        : "b" (&_top), "r" (first), "b" (last)
        : "cr0", "memory"
    );

#elif defined(__ppc__)

    AtomicNode* top;
    AtomicNode* tmp;

    __asm__ __volatile__
    (
        "lwz\t%0, 0(%4)\n\t"
        "0:\n\t"
        "mr\t\t%3, %0\n\t"
        "stw    %0, 0(%6)\n\t"
        "lwsync\n\t"
        "lwarx\t%0, 0, %4\n\t"
        "cmpw\t%0, %3\n\t"
        "bne-\t0b\n\t"
        "stwcx.\t%5, 0, %4\n\t"
        "bne-\t0b\n\t"

        : "=&b" (top), "=m" (*last), "+m" (_top), "=&b" (tmp)
        : "b" (&_top), "r" (first), "b" (last)
        : "cr0", "memory"
    );

#else

#   error

#endif
}

AtomicNode* AtomicStack::Pop()
{
#if defined(ATOMIC_HAS_DCAS)

    atomic_word2 top = atomic_load_explicit(&_top, memory_order_relaxed);
    atomic_word2 newtop;
    AtomicNode* node;

    do
    {
        node = (AtomicNode*)top.lo;
        if (!node)
            break;
        newtop.lo = (atomic_word)atomic_load_explicit(&node->_next, memory_order_relaxed);
        newtop.hi = top.hi + 1;
    }
    while (!atomic_compare_exchange_strong_explicit(&_top, &top, newtop, memory_order_acquire, memory_order_relaxed));

    return node;

#elif defined(UNITY_ATOMICQUEUE_USE_CLANG_ARM_ATOMICS)

    AtomicNode* node = 0;
    do
    {
        node = (AtomicNode*)UNITY_ATOMIC_ARMV8_LDAEX(&_top);
        UNITY_ATOMIC_ARMV7_DMB_ISH
        if (node == 0)   // stack became empty in the meantime so return null
        {
            __builtin_arm_clrex();
            break;
        }
    }
    while (UNITY_ATOMIC_ARMV8_STLEX(node->_next, &_top));
    UNITY_ATOMIC_ARMV7_DMB_ISH

    return node;

#elif defined(__arm64__)

    AtomicNode* top;
    AtomicNode* tmp;
    long success;

    __asm__ __volatile__
    (
        "0:\n\t"
        "ldaxr\t%0, [%4]\n\t"
        "cbz\t%0, 1f\n\t"
        "ldr    %1, [%0]\n\t"
        "stxr\t%w3, %1, [%4]\n\t"
        "cbnz\t%w3, 0b\n\t"
        "1:\n\t"

        : "=&r" (top), "=&r" (tmp), "+m" (_top), "=&r" (success)
        : "r" (&_top)
        : "cc", "memory"
    );
    return top;

#elif defined(__arm__)

    AtomicNode* top;
    AtomicNode* tmp;
    int success;

    __asm__ __volatile__
    (
        "0:\n\t"
        "ldrex\t%0, [%4]\n\t"
        "cmp\t%0, #0\n\t"
        "beq\t1f\n\t"
        "ldr    %1, [%0]\n\t"
        "strex\t%3, %1, [%4]\n\t"
        "teq\t%3, #0\n\t"
        "bne\t0b\n\t"
        "isb\n\t"
        "1:\n\t"

        : "=&r" (top), "=&r" (tmp), "+m" (_top), "=&r" (success)
        : "r" (&_top)
        : "cc", "memory"
    );
    return top;

#elif defined(__ppc64__) || defined(_ARCH_PPC64)

    AtomicNode* top;
    AtomicNode* tmp;

    __asm__ __volatile__
    (
        "0:\n\t"
        "ldarx\t%0, 0, %3\n\t"
        "cmpdi  %0, 0\n\t"
        "beq-   1f\n\t"
        "ld     %1, 0(%0)\n\t"
        "stdcx. %1, 0, %3\n\t"
        "bne-   0b\n\t"
        "isync\n\t"
        "b\t\t2f\n\f"
        "1:\n\t"
        "stdcx.\t%0, 0, %3\n\t"
        "bne-\t0b\n\t"
        "2:\n\t"

        : "=&b" (top), "=&b" (tmp), "+m" (_top)
        : "b" (&_top)
        : "cr0", "memory"
    );
    return top;

#elif defined(__ppc__)

    AtomicNode* top;
    AtomicNode* tmp;

    __asm__ __volatile__
    (
        "0:\n\t"
        "lwarx\t%0, 0, %3\n\t"
        "cmpwi   %0, 0\n\t"
        "beq-   1f\n\t"
        "lwz    %1, 0(%0)\n\t"
        "stwcx. %1, 0, %3\n\t"
        "bne-   0b\n\t"
        "isync\n\t"
        "b\t\t2f\n\f"
        "1:\n\t"
        "stwcx.\t%0, 0, %3\n\t"
        "bne-\t0b\n\t"
        "2:\n\t"

        : "=&b" (top), "=&b" (tmp), "+m" (_top)
        : "b" (&_top)
        : "cr0", "memory"
    );
    return top;

#else

#   error

#endif
}

AtomicNode* AtomicStack::PopAll()
{
#if defined(ATOMIC_HAS_DCAS)

    atomic_word2 top = atomic_load_explicit(&_top, memory_order_relaxed);
    atomic_word2 newtop;
    AtomicNode* node;

    do
    {
        node = (AtomicNode*)top.lo;
        if (!node)
            break;
        newtop.lo = 0;
        newtop.hi = top.hi + 1;
    }
    while (!atomic_compare_exchange_strong_explicit(&_top, &top, newtop, memory_order_acquire, memory_order_relaxed));

    return node;

#elif defined(UNITY_ATOMICQUEUE_USE_CLANG_ARM_ATOMICS)

    AtomicNode* node = 0;
    do
    {
        node = (AtomicNode*)UNITY_ATOMIC_ARMV8_LDAEX(&_top);
        UNITY_ATOMIC_ARMV7_DMB_ISH
        if (node == 0)   // stack became empty in the meantime so return null
        {
            __builtin_arm_clrex();
            break;
        }
    }
    while (UNITY_ATOMIC_ARMV8_STLEX((atomic_word)0, &_top));
    UNITY_ATOMIC_ARMV7_DMB_ISH

    return node;

#elif defined(__arm64__)

    AtomicNode* top;
    AtomicNode* tmp;
    long success;

    __asm__ __volatile__
    (
        "0:\n\t"
        "ldaxr\t%0, [%4]\n\t"
        "cbz\t%0, 1f\n\t"
        "stxr\t%w3, %5, [%4]\n\t"
        "cbnz\t%w3, 0b\n\t"
        "1:\n\t"

        : "=&r" (top), "=&r" (tmp), "+m" (_top), "=&r" (success)
        : "r" (&_top), "r" (0)
        : "cc", "memory"
    );
    return top;

#elif defined(__arm__)

    AtomicNode* top;
    AtomicNode* tmp;
    int success;

    __asm__ __volatile__
    (
        "0:\n\t"
        "ldrex\t%0, [%4]\n\t"
        "cmp\t%0, #0\n\t"
        "beq\t1f\n\t"
        "strex\t%3, %5, [%4]\n\t"
        "teq\t%3, #0\n\t"
        "bne\t0b\n\t"
        "isb\n\t"
        "1:\n\t"

        : "=&r" (top), "=&r" (tmp), "+m" (_top), "=&r" (success)
        : "r" (&_top), "r" (0)
        : "cc", "memory"
    );
    return top;

#elif defined(__ppc64__) || defined(_ARCH_PPC64)

    AtomicNode* top;
    AtomicNode* tmp;

    __asm__ __volatile__
    (
        "0:\n\t"
        "ldarx\t%0, 0, %3\n\t"
        "cmpdi  %0, 0\n\t"
        "beq-   1f\n\t"
        "stdcx. %4, 0, %3\n\t"
        "bne-   0b\n\t"
        "isync\n\t"
        "1:\n\t"

        : "=&b" (top), "=&b" (tmp), "+m" (_top)
        : "b" (&_top), "r" (0)
        : "cr0", "memory"
    );
    return top;

#elif defined(__ppc__)

    AtomicNode* top;
    AtomicNode* tmp;

    __asm__ __volatile__
    (
        "0:\n\t"
        "lwarx\t%0, 0, %3\n\t"
        "cmpwi   %0, 0\n\t"
        "beq-   1f\n\t"
        "stwcx. %4, 0, %3\n\t"
        "bne-   0b\n\t"
        "isync\n\t"
        "1:\n\t"

        : "=&b" (top), "=&b" (tmp), "+m" (_top)
        : "b" (&_top), "r" (0)
        : "cr0", "memory"
    );
    return top;

#else

#   error

#endif
}

AtomicStack* CreateAtomicStack()
{
    // should be properly aligned
#if defined(ATOMIC_HAS_DCAS)
    return UNITY_PLATFORM_NEW_ALIGNED(AtomicStack, kMemThread, sizeof(atomic_word2));
#else
    return UNITY_PLATFORM_NEW_ALIGNED(AtomicStack, kMemThread, sizeof(atomic_word));
#endif
}

void DestroyAtomicStack(AtomicStack* s)
{
    UNITY_PLATFORM_DELETE(s, kMemThread);
}

//
//  AtomicQueue
//

AtomicQueue::AtomicQueue()
{
#if defined(ATOMIC_HAS_DCAS)
    AtomicNode* dummy = UNITY_PLATFORM_NEW(AtomicNode, kMemThread);

    atomic_word2 w;
    w.lo = (atomic_word)dummy;
    w.hi = 0;
    atomic_store_explicit(&dummy->_next, 0, memory_order_relaxed);
    atomic_store_explicit(&_tail, w, memory_order_relaxed);
    atomic_store_explicit(&_head, (atomic_word)dummy, memory_order_release);

#else

    AtomicNode* dummy = UNITY_PLATFORM_NEW(AtomicNode, kMemThread);

    atomic_store_explicit(&dummy->_next, 0, memory_order_relaxed);
    atomic_store_explicit(&_tail, (atomic_word)dummy, memory_order_relaxed);
    atomic_store_explicit(&_head, (atomic_word)dummy, memory_order_release);

#endif
}

AtomicQueue::~AtomicQueue()
{
    AtomicNode* dummy = (AtomicNode*)atomic_load_explicit(&_head, memory_order_relaxed);
    UNITY_PLATFORM_DELETE(dummy, kMemThread);
}

int AtomicQueue::IsEmpty() const
{
#if defined(ATOMIC_HAS_DCAS)

    atomic_word2 cmp = atomic_load_explicit(&_tail, memory_order_relaxed);

    return atomic_load_explicit(&((AtomicNode*)cmp.lo)->_next, memory_order_relaxed) == 0;

#else

    atomic_word cmp = atomic_load_explicit(&_tail, memory_order_relaxed);

    return atomic_load_explicit(&((AtomicNode*)cmp)->_next, memory_order_relaxed) == 0;

#endif
}

void AtomicQueue::Enqueue(AtomicNode* node)
{
    // i am not sure why this is marked with "need DCAS". Also, (at least arm) asm provided is not exactly equivalent to cpp code
#if defined(ATOMIC_HAS_DCAS) || defined(UNITY_ATOMICQUEUE_USE_CLANG_ARM_ATOMICS)

    AtomicNode* prev;

    atomic_store_explicit(&node->_next, 0, memory_order_relaxed);
    prev = (AtomicNode*)atomic_exchange_explicit(&_head, (atomic_word)node, memory_order_release);
    atomic_store_explicit(&prev->_next, (atomic_word)node, memory_order_release);

#elif defined(__arm64__)

    AtomicNode* head;
    long success;

    __asm__ __volatile__
    (
        "0:\n\t"
        "ldxr\t%0, [%3]\n\t"
        "stlxr\t%w2, %4, [%3]\n\t"
        "cbnz\t%w2, 0b\n\t"
        "str    %4, [%0]\n\t"

        : "=&r" (head), "+m" (_head), "=&r" (success)
        : "r" (&_head), "r" (node)
        : "cc", "memory"
    );

#elif defined(__arm__)

    AtomicNode* head;
    int success;

    __asm__ __volatile__
    (
        "dmb\tish\n\t"
        "0:\n\t"
        "ldrex\t%0, [%3]\n\t"
        "strex\t%2, %4, [%3]\n\t"
        "teq    %2, #0\n\t"
        "bne\t0b\n\t"
        "str    %4, [%0]\n\t"

        : "=&r" (head), "+m" (_head), "=&r" (success)
        : "r" (&_head), "r" (node)
        : "cc", "memory"
    );

#elif defined(__ppc64__) || defined(_ARCH_PPC64)

    AtomicNode* head;

    __asm__ __volatile__
    (
        "lwsync\n\t"
        "0:\n\t"
        "ldarx\t%0, 0, %2\n\t"
        "stdcx.\t%3, 0, %2\n\t"
        "bne-\t0b\n\t"
        "std    %3, 0(%0)\n\t"

        : "=&b" (head), "+m" (_head)
        : "b" (&_head), "r" (node)
        : "cr0", "memory"
    );

#elif defined(__ppc__)

    AtomicNode* head;

    __asm__ __volatile__
    (
        "lwsync\n\t"
        "0:\n\t"
        "lwarx\t%0, 0, %2\n\t"
        "stwcx.\t%3, 0, %2\n\t"
        "bne-\t0b\n\t"
        "stw    %3, 0(%0)\n\t"

        : "=&b" (head), "+m" (_head)
        : "b" (&_head), "r" (node)
        : "cr0", "memory"
    );

#else

#   error

#endif
}

void AtomicQueue::EnqueueAll(AtomicNode* first, AtomicNode* last)
{
    atomic_store_explicit(&last->_next, 0, memory_order_relaxed);

    // i am not sure why this is marked with "need DCAS". Also, (at least arm) asm provided is not exactly equivalent to cpp code
#if defined(ATOMIC_HAS_DCAS) || defined(UNITY_ATOMICQUEUE_USE_CLANG_ARM_ATOMICS)

    AtomicNode* prev;

    prev = (AtomicNode*)atomic_exchange_explicit(&_head, (atomic_word)last, memory_order_release);
    atomic_store_explicit(&prev->_next, (atomic_word)first, memory_order_release);

#elif defined(__arm64__)

    AtomicNode* head;
    long success;

    __asm__ __volatile__
    (
        "0:\n\t"
        "ldxr\t%0, [%3]\n\t"
        "stlxr\t%w2, %5, [%3]\n\t"
        "cbnz\t%w2, 0b\n\t"
        "str    %4, [%0]\n\t"

        : "=&r" (head), "+m" (_head), "=&r" (success)
        : "r" (&_head), "r" (first), "r" (last)
        : "cc", "memory"
    );

#elif defined(__arm__)

    AtomicNode* head;
    int success;

    __asm__ __volatile__
    (
        "dmb\tish\n\t"
        "0:\n\t"
        "ldrex\t%0, [%3]\n\t"
        "strex\t%2, %5, [%3]\n\t"
        "teq    %2, #0\n\t"
        "bne\t0b\n\t"
        "str    %4, [%0]\n\t"

        : "=&r" (head), "+m" (_head), "=&r" (success)
        : "r" (&_head), "r" (first), "r" (last)
        : "cc", "memory"
    );

#elif defined(__ppc64__) || defined(_ARCH_PPC64)

    AtomicNode* head;

    __asm__ __volatile__
    (
        "lwsync\n\t"
        "0:\n\t"
        "ldarx\t%0, 0, %2\n\t"
        "stdcx.\t%4, 0, %2\n\t"
        "bne-\t0b\n\t"
        "std    %3, 0(%0)\n\t"

        : "=&b" (head), "+m" (_head)
        : "b" (&_head), "r" (first), "r" (last)
        : "cr0", "memory"
    );

#elif defined(__ppc__)

    AtomicNode* head;

    __asm__ __volatile__
    (
        "lwsync\n\t"
        "0:\n\t"
        "lwarx\t%0, 0, %2\n\t"
        "stwcx.\t%4, 0, %2\n\t"
        "bne-\t0b\n\t"
        "stw    %3, 0(%0)\n\t"

        : "=&b" (head), "+m" (_head)
        : "b" (&_head), "r" (first), "r" (last)
        : "cr0", "memory"
    );

#else

#   error

#endif
}

AtomicNode* AtomicQueue::Dequeue()
{
#if defined(ATOMIC_HAS_DCAS)

    AtomicNode* res, * next;
    void* data0;
    void* data1;
    void* data2;

    atomic_word2 tail = atomic_load_explicit(&_tail, memory_order_acquire);
    atomic_word2 newtail;
    do
    {
        res = (AtomicNode*)tail.lo;
        next = (AtomicNode*)atomic_load_explicit(&res->_next, memory_order_relaxed);
        if (next == 0)
            return NULL;
        data0 = next->data[0];
        data1 = next->data[1];
        data2 = next->data[2];
        newtail.lo = (atomic_word)next;
        newtail.hi = tail.hi + 1;
    }
    while (!atomic_compare_exchange_strong_explicit(&_tail, &tail, newtail, memory_order_seq_cst, memory_order_relaxed));

    res->data[0] = data0;
    res->data[1] = data1;
    res->data[2] = data2;

    return res;

#elif defined(UNITY_ATOMICQUEUE_USE_CLANG_ARM_ATOMICS)

    AtomicNode *ret = 0, *next = 0; void* data[3];
    do
    {
        ret = (AtomicNode*)UNITY_ATOMIC_ARMV8_LDAEX(&_tail);
        UNITY_ATOMIC_ARMV7_DMB_ISH

            next = (AtomicNode*)ret->_next;
        if (!next) // empty queue
        {
            __builtin_arm_clrex();
            return 0;
        }
        data[0] = next->data[0], data[1] = next->data[1], data[2] = next->data[2];
    }
    while (UNITY_ATOMIC_ARMV8_STLEX((atomic_word)next, &_tail));
    UNITY_ATOMIC_ARMV7_DMB_ISH

    ret->data[0] = data[0], ret->data[1] = data[1], ret->data[2] = data[2];
    return ret;

#elif defined(__arm64__)

    AtomicNode* tail;
    AtomicNode* tmp;
    void* data0;
    void* data1;
    void* data2;
    long success;

    __asm__ __volatile__ (
        "0:\n\t"
        "ldaxr\t%0, [%7]\n\t"
        "ldr\t%1, [%0]\n\t"
        "cbz\t%1, 1f\n\t"
        "ldr    %4, [%1, #8]\n\t"
        "ldr    %5, [%1, #16]\n\t"
        "ldr    %6, [%1, #24]\n\t"
        // create an artificial dependency to ensure previous loads are not reordered
        "csel\t%7, %7, %3, #15\n\t" // nop
        "csel\t%7, %7, %4, #15\n\t" // nop
        "csel\t%7, %7, %5, #15\n\t" // nop
        "stxr\t%w3, %1, [%7]\n\t"
        "cbnz\t%w3, 0b\n\t"
        "1:\n\t"

        : "=&r" (tail), "=&r" (tmp), "+m" (_tail), "=&r" (success), "=&r" (data0), "=&r" (data1), "=&r" (data2)
        : "r" (&_tail)
        : "cc", "memory"
    );
    if (tmp)
    {
        tail->data[0] = data0;
        tail->data[1] = data1;
        tail->data[2] = data2;
    }
    else
    {
        tail = 0;
    }
    return tail;

#elif defined(__arm__)

    AtomicNode* tail;
    AtomicNode* tmp;
    void* data0;
    void* data1;
    void* data2;
    int success;

    __asm__ __volatile__ (
        "0:\n\t"
        "ldrex\t%0, [%7]\n\t"
        "ldr\t%1, [%0]\n\t"
        "cmp\t%1, #0\n\t"
        "beq\t1f\n\t"
        "ldr\t%4, [%1, #4]\n\t"
        "ldr\t%5, [%1, #8]\n\t"
        "ldr\t%6, [%1, #12]\n\t"
        // create an artificial dependency to ensure previous loads are not reordered
        "orr\t%7, %7, %3, lsr #32\n\t" // nop
        "orr\t%7, %7, %4, lsr #32\n\t" // nop
        "orr\t%7, %7, %5, lsr #32\n\t" // nop
        "strex\t%3, %1, [%7]\n\t"
        "teq\t%3, #0\n\t"
        "bne\t0b\n\t"
        "isb\n\t"
        "1:\n\t"

        : "=&r" (tail), "=&r" (tmp), "+m" (_tail), "=&r" (success), "=&r" (tail), "=&r" (data0), "=&r" (data1), "=&r" (data2)
        : "r" (&_tail)
        : "cc", "memory"
    );
    if (tmp)
    {
        tail->data[0] = data0;
        tail->data[1] = data1;
        tail->data[2] = data2;
    }
    else
    {
        tail = 0;
    }
    return tail;

#elif defined(__ppc64__) || defined(_ARCH_PPC64)

    AtomicNode* tail;
    AtomicNode* tmp;
    void* data0;
    void* data1;
    void* data2;

    __asm__ __volatile__
    (
        "0:\n\t"
        "ldarx\t%0, 0, %6\n\t"
        "ld     %1, 0(%0)\n\t"
        "cmpdi  %1, 0\n\t"
        "beq-   1f\n\t"
        "ld     %3, 8(%1)\n\t"
        "ld     %4, 16(%1)\n\t"
        "ld     %5, 24(%1)\n\t"
        // create a data dependency to ensure previous loads are not reordered
        "tdi\t0, %4, 0\n\t"
        "tdi\t0, %5, 0\n\t"
        "tdi\t0, %6, 0\n\t"
        "isync\n\t"
        "stdcx. %1, 0, %6\n\t"
        "bne-   0b\n\t"
        "isync\n\t"
        "1:\n\t"

        : "=&b" (tail), "=&b" (tmp), "+m" (_tail), "=&b" (data0), "=&b" (data1), "=&b" (data2)
        : "b" (&_tail)
        : "cr0", "memory"
    );
    if (tmp)
    {
        tail->data[0] = data0;
        tail->data[1] = data1;
        tail->data[2] = data2;
    }
    else
    {
        tail = 0;
    }
    return tail;

#elif defined(__ppc__)

    AtomicNode* tail;
    AtomicNode* tmp;
    void* data0;
    void* data1;
    void* data2;

    __asm__ __volatile__ (
        "0:\n\t"
        "lwarx\t%0, 0, %6\n\t"
        "lwz    %1, 0(%0)\n\t"
        "cmpwi  %1, 0\n\t"
        "beq-   1f\n\t"
        "lwz    %3, 4(%1)\n\t"
        "lwz    %4, 8(%1)\n\t"
        "lwz    %5, 12(%1)\n\t"
        // create a data dependency to ensure previous loads are not reordered
        "twi\t0, %4, 0\n\t"
        "twi\t0, %5, 0\n\t"
        "twi\t0, %6, 0\n\t"
        "isync\n\t"
        "stwcx. %1, 0, %6\n\t"
        "bne-   0b\n\t"
        "isync\n\t"
        "1:\n\t"

        : "=&b" (tail), "=&b" (tmp), "+m" (_tail), "=&b" (data0), "=&b" (data1), "=&b" (data2)
        : "b" (&_tail)
        : "cr0", "memory"
    );
    if (tmp)
    {
        tail->data[0] = data0;
        tail->data[1] = data1;
        tail->data[2] = data2;
    }
    else
    {
        tail = 0;
    }
    return tail;

#else

#   error

#endif
}

AtomicQueue* CreateAtomicQueue()
{
    // UNITY_NEW can be used when it starts supporting alignof (instead of default alignement)
    return UNITY_PLATFORM_NEW_ALIGNED(AtomicQueue, kMemThread, sizeof(atomic_word2));
}

void DestroyAtomicQueue(AtomicQueue* s)
{
    UNITY_PLATFORM_DELETE(s, kMemThread);
}

//
//  AtomicList
//

void AtomicList::Init()
{
#if defined(ATOMIC_HAS_DCAS) || defined(UNITY_ATOMICQUEUE_USE_CLANG_ARM_ATOMICS)

    atomic_word2 w;
    w.lo = w.hi = 0;
    atomic_store_explicit(&_top, w, memory_order_relaxed);

#else

    atomic_store_explicit(&_top, 0, memory_order_relaxed);
    atomic_store_explicit(&_ver, 0, memory_order_relaxed);

#endif
}

atomic_word AtomicList::Tag()
{
#if defined(ATOMIC_HAS_DCAS) || defined(UNITY_ATOMICQUEUE_USE_CLANG_ARM_ATOMICS)

    atomic_word2 w = atomic_load_explicit((volatile atomic_word2*)&_top, memory_order_relaxed);

    return w.hi;

#else

    return atomic_load_explicit(&_ver, memory_order_relaxed);

#endif
}

AtomicNode* AtomicList::Peek()
{
#if defined(ATOMIC_HAS_DCAS) || defined(UNITY_ATOMICQUEUE_USE_CLANG_ARM_ATOMICS)

    atomic_word2 w = atomic_load_explicit((volatile atomic_word2*)&_top, memory_order_relaxed);

    return (AtomicNode*)w.lo;

#else

    return (AtomicNode*)atomic_load_explicit(&_top, memory_order_relaxed);

#endif
}

AtomicNode* AtomicList::Load(atomic_word &tag)
{
#if defined(ATOMIC_HAS_DCAS) || defined(UNITY_ATOMICQUEUE_USE_CLANG_ARM_ATOMICS)

    atomic_word2 w = atomic_load_explicit((volatile atomic_word2*)&_top, memory_order_acquire);
    tag = w.hi;

    return (AtomicNode*)w.lo;

#else

    atomic_word w;
    do
    {
        w = atomic_load_explicit(&_top, memory_order_relaxed);
        tag = atomic_load_explicit(&_ver, memory_order_relaxed);
    }
    while (tag != atomic_load_explicit(&_ver, memory_order_acquire));

    return (AtomicNode*)w;

#endif
}

bool AtomicList::Add(AtomicNode *first, AtomicNode *last, atomic_word tag)
{
#if defined(ATOMIC_HAS_DCAS) || defined(UNITY_ATOMICQUEUE_USE_CLANG_ARM_ATOMICS)

    atomic_word2 oldval, newval;
    bool res = false;

    newval.lo = (atomic_word)first;
    newval.hi = tag;

    oldval = atomic_load_explicit((volatile const atomic_word2*)&_top, memory_order_relaxed);
    while (oldval.hi == tag)
    {
        last->Link((AtomicNode*)oldval.lo);
        res = atomic_compare_exchange_strong_explicit((volatile atomic_word2*)&_top, &oldval, newval, memory_order_acq_rel, memory_order_relaxed);
        if (res)
        {
            break;
        }
    }
    return res;

#elif defined(__arm64__)

    AtomicNode* res;
    AtomicNode* tmp;
    long failure = 1;

    __asm__ __volatile__
    (
        "0:\n\t"
        "ldxr\t%0, [%4]\n\t"
        "csel\t%4, %4, %0, #15\n\t" // nop
        "ldr\t%1, [%4, #8]\n\t"
        "cmp\t%1, %7\n\t"
        "b.ne\t1f\n\t"
        "str    %0, [%6]\n\t"
        "stlxr\t%w3, %5, [%4]\n\n"
        "cbnz\t%w3, 0b\n\t"
        "1:\n\t"

        : "=&r" (res), "=&r" (tmp), "+m" (_top), "=&r" (failure)
        : "r" (&_top), "r" (first), "r" (last), "r" (tag)
        : "cc", "memory"
    );
    return failure == 0;

#elif defined(__arm__)

    AtomicNode* res;
    AtomicNode* tmp;
    int failure = 1;

    __asm__ __volatile__
    (
        "dmb\tishst\n\t"
        "0:\n\t"
        "ldrex\t%0, [%4]\n\t"
        "add\t%4, %4, %0\n\t"   // nop
        "sub\t%4, %4, %0\n\t" // nop
        "ldr\t%1, [%4, #4]\n\t"
        "cmp\t%1, %7\n\t"
        "bne\t1f\n\t"
        "str\t%0, [%6]\n\t"
        "strex\t%3, %5, [%4]\n\t"
        "teq\t%3, #0\n\t"
        "bne\t0b\n\t"
        "1:\n\t"

        : "=&r" (res), "=&r" (tmp), "+m" (_top), "=&r" (failure)
        : "r" (&_top), "r" (first), "r" (last), "r" (tag)
        : "cc", "memory"
    );
    return failure == 0;

#elif defined(__ppc__)

    AtomicNode* res;
    AtomicNode* tmp;
    int failure = 1;

    __asm__ __volatile__
    (
        "0:\n\t"
        "lwsync\n\t"
        "lwarx\t%0, 0, %4\n\t"
        "add\t%4, %4, %0\n\t"   // nop
        "sub\t%4, %4, %0\n\t" // nop
        "lwz\t%1, 4(%4)\n\t"
        "cmpw\t%1, %7\n\t"
        "bne-\t1f\n\t"
        "std\t%0, 0(%6)\n\t"
        "stwcx.\t%5, 0, %4\n\t"
        "bne-\t0b\n\t"
        "eor\t%3, %3, %3\n\t"
        "1:\n\t"

        : "=&r" (res), "=&r" (tmp), "+m" (_top), "=&r" (failure)
        : "r" (&_top), "r" (first), "r" (last), "r" (tag)
        : "cr0", "memory"
    );
    return failure == 0;

#elif defined(__ppc64__) || defined(_ARCH_PPC64)

    AtomicNode* res;
    AtomicNode* tmp;
    int failure = 1;

    __asm__ __volatile__
    (
        "lwsync\n\t"
        "0:\n\t"
        "ldarx\t%0, 0, %4\n\t"
        "add\t%4, %4, %0\n\t"   // nop
        "sub\t%4, %4, %0\n\t" // nop
        "ld\t\t%1, 8(%4)\n\t"
        "cmpd\t%1, %7\n\t"
        "bne-\t1f\n\t"
        "std\t%0, 0(%6)\n\t"
        "stdcx.\t%5, 0, %4\n\t"
        "bne-\t0b\n\t"
        "eord\t%3, %3, %3\n\t"
        "1:\n\t"

        : "=&r" (res), "=&r" (tmp), "+m" (_top), "=&r" (failure)
        : "r" (&_top), "r" (first), "r" (last), "r" (tag)
        : "cr0", "memory"
    );
    return failure == 0;

#else

#   error

#endif
}

AtomicNode* AtomicList::Touch(atomic_word tag)
{
#if defined(ATOMIC_HAS_DCAS) || defined(UNITY_ATOMICQUEUE_USE_CLANG_ARM_ATOMICS)

    atomic_word2 w;
    w.lo = 0;
    w.hi = tag;
    w = atomic_exchange_explicit((volatile atomic_word2*)&_top, w, memory_order_acq_rel);

    return (AtomicNode*)w.lo;

#else

    atomic_store_explicit(&_ver, tag, memory_order_release);
    atomic_word w = atomic_exchange_explicit(&_top, 0, memory_order_acquire);

    return (AtomicNode*)w;

#endif
}

void AtomicList::Reset(AtomicNode* node, atomic_word tag)
{
#if defined(ATOMIC_HAS_DCAS) || defined(UNITY_ATOMICQUEUE_USE_CLANG_ARM_ATOMICS)

    atomic_word2 w;
    w.lo = (atomic_word)node;
    w.hi = tag;
    atomic_store_explicit(&_top, w, memory_order_relaxed);

#else

    atomic_store_explicit(&_top, (atomic_word)node, memory_order_relaxed);
    atomic_store_explicit(&_ver, tag, memory_order_relaxed);

#endif
}

AtomicNode* AtomicList::Clear(AtomicNode* old, atomic_word tag)
{
#if defined(ATOMIC_HAS_DCAS) || defined(UNITY_ATOMICQUEUE_USE_CLANG_ARM_ATOMICS)

    atomic_word2 top;
    atomic_word2 newtop;
    top.lo = (atomic_word)old;
    top.hi = tag;
    newtop.lo = 0;
    newtop.hi = tag + 1;

    if (atomic_compare_exchange_strong_explicit((volatile atomic_word2*)&_top, &top, newtop, memory_order_acquire, memory_order_relaxed))
    {
        return (AtomicNode*)top.lo;
    }
    else
    {
        return NULL;
    }

#elif defined(__arm64__)

    AtomicNode* res;
    AtomicNode* tmp;
    long success;

    __asm__ __volatile__
    (
        "0:\n\t"
        "ldaxr\t%0, [%4]\n\t"
        "cbz\t%0, 2f\n\t"
        "ldr    %1, [%0]\n\t"
        "stxr\t%w3, %1, [%4]\n\t"
        "cbnz\t%w3, 0b\n\t"
        "1:\n\t"
        "ldxr\t%1, [%4, #8]\n\t"
        "add\t%1, %1, 1\n\t"
        "stxr\t%w3, %1, [%4, #8]\n\t"
        "cbnz\t%w3, 1b\n\t"
        "2:\n\t"

        : "=&r" (res), "=&r" (tmp), "+m" (_top), "=&r" (success)
        : "r" (&_top)
        : "cc", "memory"
    );
    return res;

#elif defined(__arm__)

    AtomicNode* res;
    AtomicNode* tmp;
    int success;

    __asm__ __volatile__
    (
        "0:\n\t"
        "ldrex\t%0, [%4]\n\t"
        "cmp\t%0, #0\n\t"
        "beq\t2f\n\t"
        "ldr    %1, [%0]\n\t"
        "strex\t%3, %1, [%4]\n\t"
        "teq\t%3, #0\n\t"
        "bne\t0b\n\t"
        "add\t%4, %4, 4\n\t"
        "1:\n\t"
        "ldrex\t%3, [%4]\n\t"
        "add\t%3, %3, 1\n\t"
        "strex\t%3, %3, [%4]\n\t"
        "teq\t%3, #0\n\t"
        "bne\t1b\n\t"
        "sub\t%4, %4, 4\n\t"
        "isb\n\t"
        "2:\n\t"

        : "=&r" (res), "=&r" (tmp), "+m" (_top), "=&r" (success)
        : "r" (&_top)
        : "cc", "memory"
    );
    return res;

#elif defined(__ppc64__) || defined(_ARCH_PPC64)

    AtomicNode* res;
    AtomicNode* tmp;

    __asm__ __volatile__
    (
        "0:\n\t"
        "ldarx\t%0, 0, %3\n\t"
        "cmpdi  %0, 0\n\t"
        "beq-   2f\n\t"
        "ld     %1, 0(%0)\n\t"
        "stdcx. %1, 0, %3\n\t"
        "bne-   0b\n\t"
        "1:\n\t"
        "ldarx\t%1, 8, %3\n\t"
        "addi\t%1, %1, 1\n\t"
        "stdcx.\t%1, 8, %3\n\t"
        "bne-\t1b\n\t"
        "isync\n\t"
        "b\t\t3f\n\f"
        "2:\n\t"
        "stdcx.\t%0, 0, %3\n\t"
        "bne-\t0b\n\t"
        "3:\n\t"

        : "=&b" (res), "=&b" (tmp), "+m" (_top)
        : "b" (&_top)
        : "cr0", "memory"
    );
    return res;

#elif defined(__ppc__)

    AtomicNode* res;
    AtomicNode* tmp;

    __asm__ __volatile__
    (
        "0:\n\t"
        "lwarx\t%0, 0, %3\n\t"
        "cmpwi   %0, 0\n\t"
        "beq-   2f\n\t"
        "lwz    %1, 0(%0)\n\t"
        "stwcx. %1, 0, %3\n\t"
        "bne-   0b\n\t"
        "1:\n\t"
        "lwarx\t%1, 4, %3\n\t"
        "addiw\t%1, %1, 1\n\t"
        "stwcx.\t%1, 4, %3\n\t"
        "bne-\t1b\n\t"
        "isync\n\t"
        "b\t\t3f\n\f"
        "2:\n\t"
        "stwcx.\t%0, 0, %3\n\t"
        "bne-\t0b\n\t"
        "3:\n\t"

        : "=&b" (res), "=&b" (tmp), "+m" (_top)
        : "b" (&_top)
        : "cr0", "memory"
    );
    return res;

#else

#   error

#endif
}

#else // !ATOMIC_HAS_QUEUE

//
//  AtomicList
//

void AtomicList::Init()
{
    _top = 0;
    _ver = 0;
}

atomic_word AtomicList::Tag()
{
    return _ver;
}

AtomicNode* AtomicList::Peek()
{
    return (AtomicNode*)_top;
}

AtomicNode* AtomicList::Load(atomic_word &tag)
{
    tag = _ver;
    return (AtomicNode*)_top;
}

bool AtomicList::Add(AtomicNode *first, AtomicNode *last, atomic_word tag)
{
    last->Link((AtomicNode*)_top);
    _top = (atomic_word)first;
    return true;
}

AtomicNode* AtomicList::Touch(atomic_word tag)
{
    AtomicNode* res = (AtomicNode*)_top;
    _top = 0;
    _ver = tag;
    return res;
}

void AtomicList::Reset(AtomicNode* node, atomic_word tag)
{
    _top = (atomic_word)node;
    _ver = tag;
}

AtomicNode* AtomicList::Clear(AtomicNode* old, atomic_word tag)
{
    if (_top == (atomic_word)old && _ver == tag)
    {
        _top = 0;
        ++_ver;
        return old;
    }
    else
    {
        return NULL;
    }
}

#endif

#endif // !IL2CPP_TARGET_JAVASCRIPT

UNITY_PLATFORM_END_NAMESPACE;

#endif
