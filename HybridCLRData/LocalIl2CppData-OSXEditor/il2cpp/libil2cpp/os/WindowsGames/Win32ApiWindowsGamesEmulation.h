#pragma once


#if IL2CPP_TARGET_WINDOWS_GAMES

#include "os/Win32/WindowsHeaders.h"
#include <WinSock2.h>
#include "os/ErrorCodes.h"

extern "C"
{
#define GetLongPathName GetLongPathNameW
#define GetUserName GetUserNameW
#define ReplaceFile ReplaceFileW

    typedef
        BOOL
    (PASCAL FAR * LPFN_DISCONNECTEX)(
        _In_ SOCKET s,
        _Inout_opt_ LPOVERLAPPED lpOverlapped,
        _In_ DWORD  dwFlags,
        _In_ DWORD  dwReserved
    );

#define WSAID_DISCONNECTEX \
    {0x7fda2e11,0x8630,0x436f,{0xa0, 0x31, 0xf5, 0x36, 0xa6, 0xee, 0xc1, 0x57}}

    inline DWORD WINAPI GetTempPathW(DWORD nBufferLength, LPWSTR lpBuffer)
    {
        const wchar_t* name = L"T:\\";
        const size_t nameLen = wcslen(name);

        memcpy(lpBuffer, name, nameLen * sizeof(wchar_t));
        lpBuffer[nameLen] = L'\0';
        return (DWORD)nameLen;
    }

    inline DWORD WINAPI GetLongPathNameW(LPCWSTR lpszShortPath, LPWSTR lpszLongPath, DWORD cchBuffer)
    {
        memmove(lpszLongPath, lpszShortPath, cchBuffer * sizeof(wchar_t));
        return cchBuffer;
    }

    inline BOOL WINAPI ReplaceFileW(LPCWSTR lpReplacedFileName, LPCWSTR lpReplacementFileName, LPCWSTR lpBackupFileName, DWORD dwReplaceFlags, LPVOID lpExclude, LPVOID lpReserved)
    {
        SetLastError(il2cpp::os::kErrorCallNotImplemented);
        return FALSE;
    }

    inline BOOL WINAPI LockFileEx(HANDLE hFile, DWORD dwFlags, DWORD dwReserved, DWORD nNumberOfBytesToLockLow, DWORD nNumberOfBytesToLockHigh, LPOVERLAPPED lpOverlapped)
    {
        SetLastError(il2cpp::os::kErrorCallNotImplemented);
        return FALSE;
    }

    inline BOOL WINAPI UnlockFileEx(HANDLE hFile, DWORD dwReserved, DWORD nNumberOfBytesToUnlockLow, DWORD nNumberOfBytesToUnlockHigh, LPOVERLAPPED lpOverlapped)
    {
        SetLastError(il2cpp::os::kErrorCallNotImplemented);
        return FALSE;
    }

    inline HANDLE WINAPI CreateSemaphore(LPSECURITY_ATTRIBUTES lpSemaphoreAttributes, LONG lInitialCount, LONG lMaximumCount, LPCWSTR lpName)
    {
        return ::CreateSemaphoreEx(lpSemaphoreAttributes, lInitialCount, lMaximumCount, lpName, 0, SEMAPHORE_ALL_ACCESS);
    }

    inline BOOL WINAPI GetUserNameW(LPWSTR lpBuffer, LPDWORD pcbBuffer)
    {
        SetLastError(il2cpp::os::kErrorCallNotImplemented);
        return FALSE;
    }
}


#endif //IL2CPP_TARGET_WINDOWS_GAMES
