using Orchestra.Api.Common;
using Orchestra.Api.Common.Hosting;

namespace Orchestra.Api.Tests.Common;

public sealed class PathMapperTests
{
    private static PathMapper Mapper(params (string Host, string Server)[] entries)
    {
        return new PathMapper(entries.Select(entry => new PathMapEntry { Host = entry.Host, Server = entry.Server }));
    }

    [Fact]
    public void The_longest_host_prefix_wins()
    {
        var mapper = Mapper(("F:/Projects", "/host"), ("F:/Projects/Mine", "/host/mine"));

        mapper.ToServer(@"F:\Projects\Mine\app").Should().Be("/host/mine/app");
        mapper.ToServer(@"F:\Projects\Other").Should().Be("/host/Other");
    }

    [Fact]
    public void The_host_side_ignores_case_and_accepts_both_separators()
    {
        var mapper = Mapper(("f:/projects", "/host"));

        mapper.ToServer(@"F:\PROJECTS\Shop").Should().Be("/host/Shop");
        mapper.ToServer("F:/projects/shop/").Should().Be("/host/shop");
    }

    [Fact]
    public void Whole_segments_only()
    {
        var mapper = Mapper(("F:/Proj", "/host"));

        mapper.ToServer("F:/Projects2/app").Should().Be("F:/Projects2/app");
    }

    [Fact]
    public void A_no_match_returns_the_typed_value_unchanged()
    {
        var mapper = Mapper(("F:/Projects", "/host"));

        mapper.ToServer(@"C:\Elsewhere").Should().Be(@"C:\Elsewhere");
        mapper.ToServer("  ").Should().BeEmpty();
    }

    [Fact]
    public void An_empty_map_is_the_identity()
    {
        PathMapper.Empty.ToServer(@"F:\Projects\App").Should().Be(@"F:\Projects\App");
        PathMapper.Empty.ToHost("/host/app").Should().Be("/host/app");
    }

    [Fact]
    public void To_host_is_the_inverse_and_returns_a_windows_spelling()
    {
        var mapper = Mapper(("F:/Projects", "/host"));

        mapper.ToHost("/host/Mine/app").Should().Be(@"F:\Projects\Mine\app");
        mapper.ToHost("/host").Should().Be(@"F:\Projects");
        mapper.ToHost("/elsewhere").Should().Be("/elsewhere");
        mapper.ToHost("/HOST/app").Should().Be("/HOST/app");
    }

    [Fact]
    public void A_drive_root_keeps_its_slash()
    {
        var mapper = Mapper(("F:/", "/host"));

        mapper.ToServer("F:/").Should().Be("/host");
        mapper.ToServer("F:\\").Should().Be("/host");
    }
}
