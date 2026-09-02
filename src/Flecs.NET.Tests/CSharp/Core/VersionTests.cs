using System.Runtime.InteropServices;
using Xunit;
using static Flecs.NET.Bindings.flecs;

namespace Flecs.NET.Tests.CSharp.Core;

public unsafe class VersionTests
{
    [Fact]
    private void FlecsVersion()
    {
        // ABI test: the native flecs library that Flecs.NET binds to must
        // report the version the bindings were generated for. v4 removed the
        // ecs_version() function; version info is exposed through
        // ecs_get_build_info().
        ecs_build_info_t* info = ecs_get_build_info();
        Assert.True(info != null);

        Assert.Equal("4.1.6", Marshal.PtrToStringAnsi((nint)info->version));
        Assert.Equal(4, info->version_major);
        Assert.Equal(1, info->version_minor);
        Assert.Equal(6, info->version_patch);

        // The version constants baked into the bindings must agree.
        Assert.Equal("4.1.6", FLECS_VERSION);
        Assert.Equal(4, FLECS_VERSION_MAJOR);
        Assert.Equal(1, FLECS_VERSION_MINOR);
        Assert.Equal(6, FLECS_VERSION_PATCH);
    }
}
