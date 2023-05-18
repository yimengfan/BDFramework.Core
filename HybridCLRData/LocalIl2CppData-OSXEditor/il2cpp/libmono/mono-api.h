#pragma once

extern "C"
{
#include "glib.h"
#include <mono/metadata/class-internals.h>
#include <mono/metadata/object-internals.h>

#include <mono/metadata/metadata.h>
#include <mono/metadata/object.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/runtime.h>
#include <mono/metadata/profiler-private.h>

#include <mono/metadata/handle.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/unity-utils.h>

#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-stack-unwinding.h>
#include <mono/utils/mono-context.h>
#include <mono/utils/mono-threads.h>

#include <mono/metadata/threads-types.h>
#include <mono/metadata/threadpool.h>
}
