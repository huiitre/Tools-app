using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Translation.Domain;
using Xunit;

namespace Tools.Api.UnitTests.Modules.Translation;

public sealed class TranslationRequestTests
{
    [Fact]
    public void A_valid_request_counts_the_characters_of_every_text()
    {
        var request = TranslationRequest.Create(["Bonjour", "le monde"], "fr", "en");

        Assert.Equal(15, request.Characters);
        Assert.Equal("fr", request.Source);
        Assert.Equal("en", request.Target);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("auto")]
    public void A_missing_or_auto_source_means_detection(string? source)
    {
        Assert.Null(TranslationRequest.Create(["Bonjour"], source, "en").Source);
    }

    [Fact]
    public void Exactly_the_limit_is_accepted_one_more_is_refused()
    {
        Assert.Equal(5000, TranslationRequest.Create([new string('a', 5000)], "fr", "en").Characters);

        // La limite porte sur le total du lot, pas sur chaque fragment.
        var error = Assert.Throws<AppException>(() =>
            TranslationRequest.Create([new string('a', 2500), new string('b', 2501)], "fr", "en"));
        Assert.Equal("TRANSLATION_TEXT_TOO_LONG", error.Code);
    }

    [Theory]
    [InlineData(new string[0], "TRANSLATION_TEXT_REQUIRED")]
    [InlineData(new[] { "   ", "" }, "TRANSLATION_TEXT_REQUIRED")]
    public void An_empty_text_is_refused(string[] texts, string code)
    {
        Assert.Equal(code, Assert.Throws<AppException>(() => TranslationRequest.Create(texts, "fr", "en")).Code);
    }

    [Theory]
    [InlineData("fr", null, "TRANSLATION_TARGET_REQUIRED")]
    [InlineData("fr", "fr", "TRANSLATION_SAME_LANGUAGE")]
    [InlineData("français", "en", "TRANSLATION_LANGUAGE_INVALID")]
    [InlineData("fr", "EN; DROP", "TRANSLATION_LANGUAGE_INVALID")]
    public void Invalid_languages_are_refused(string? source, string? target, string code)
    {
        Assert.Equal(code, Assert.Throws<AppException>(() => TranslationRequest.Create(["Bonjour"], source, target)).Code);
    }
}
