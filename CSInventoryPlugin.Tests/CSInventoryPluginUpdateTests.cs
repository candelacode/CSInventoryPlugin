using System;
using Xunit;
using Plugin = CSInventory.Plugin.CSInventoryPlugin;

namespace CSInventoryPlugin.Tests;

public sealed class CSInventoryPluginUpdateTests {
	[Fact]
	public void ParseTagAsVersion_LowercaseVPrefix_StripsAndParses() {
		Version version = Plugin.ParseTagAsVersion("v1.2.3.4");

		Assert.Equal(1, version.Major);
		Assert.Equal(2, version.Minor);
		Assert.Equal(3, version.Build);
		Assert.Equal(4, version.Revision);
	}

	[Fact]
	public void ParseTagAsVersion_UppercaseVPrefix_StripsAndParses() {
		Version version = Plugin.ParseTagAsVersion("V1.2.3.4");

		Assert.Equal(1, version.Major);
		Assert.Equal(2, version.Minor);
		Assert.Equal(3, version.Build);
		Assert.Equal(4, version.Revision);
	}

	[Fact]
	public void ParseTagAsVersion_SingleSegmentWithVPrefix_PadsToFourSegments() {
		Version version = Plugin.ParseTagAsVersion("v1");

		Assert.Equal(1, version.Major);
		Assert.Equal(0, version.Minor);
		Assert.Equal(0, version.Build);
		Assert.Equal(0, version.Revision);
	}

	[Fact]
	public void ParseTagAsVersion_TwoSegmentWithVPrefix_PadsToFourSegments() {
		Version version = Plugin.ParseTagAsVersion("v1.0");

		Assert.Equal(1, version.Major);
		Assert.Equal(0, version.Minor);
		Assert.Equal(0, version.Build);
		Assert.Equal(0, version.Revision);
	}

	[Fact]
	public void ParseTagAsVersion_ThreeSegmentWithVPrefix_PadsToFourSegments() {
		Version version = Plugin.ParseTagAsVersion("v1.0.0");

		Assert.Equal(1, version.Major);
		Assert.Equal(0, version.Minor);
		Assert.Equal(0, version.Build);
		Assert.Equal(0, version.Revision);
	}

	[Fact]
	public void ParseTagAsVersion_NoPrefix_ParsesVerbatim() {
		Version version = Plugin.ParseTagAsVersion("1.2.3.4");

		Assert.Equal(1, version.Major);
		Assert.Equal(2, version.Minor);
		Assert.Equal(3, version.Build);
		Assert.Equal(4, version.Revision);
	}

	[Fact]
	public void ParseTagAsVersion_PreReleaseSuffix_StripsSuffixAndParses() {
		Version version = Plugin.ParseTagAsVersion("v1.2.3.4-beta");

		Assert.Equal(1, version.Major);
		Assert.Equal(2, version.Minor);
		Assert.Equal(3, version.Build);
		Assert.Equal(4, version.Revision);
	}

	[Fact]
	public void ParseTagAsVersion_PreReleaseSuffixWithShortTag_StripsAndPads() {
		Version version = Plugin.ParseTagAsVersion("v1-beta");

		Assert.Equal(1, version.Major);
		Assert.Equal(0, version.Minor);
		Assert.Equal(0, version.Build);
		Assert.Equal(0, version.Revision);
	}

	[Fact]
	public void ParseTagAsVersion_UnparseableTag_ThrowsFormatException() {
		Assert.Throws<FormatException>(() => Plugin.ParseTagAsVersion("latest"));
	}
}
