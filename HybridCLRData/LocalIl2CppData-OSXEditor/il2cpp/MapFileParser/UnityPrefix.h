#ifndef UNITY_PREFIX_H
#define UNITY_PREFIX_H

#include <string>
#include <vector>
#include <set>
#include <map>

// These are needed for prefix configure
#define UNITY_EXTERNAL_TOOL 1
// This has to be included before defining platform macros.
#include "Configuration/PrefixConfigure.h"

#if defined(_DEBUG) && !defined(ENABLE_UNIT_TESTS)
#define ENABLE_UNIT_TESTS 1
#endif

#include "Runtime/Logging/LogAssert.h"

#endif
