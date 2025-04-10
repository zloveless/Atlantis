namespace Atlantis.Net.Irc;

using JetBrains.Annotations;

[PublicAPI]
public static class IrcClientExtensions
{
    /// <summary>
    /// Wraps the specified string with an IRC color code. 
    /// </summary>
    /// <param name="source">The string to wrap with color codes.</param>
    /// <param name="colorCode">The color between 0 and 99.</param>
    /// <returns>The string wrapped with color tokens.</returns>
    /// <exception cref="ArgumentOutOfRangeException">thrown if the specified color code was outside the range of [0..99].</exception>
    public static string Colorize(this string source, int colorCode) 
    {
        // ReSharper disable once MergeIntoLogicalPattern
        if (colorCode < 0 || colorCode > 99)
        {
            throw new ArgumentOutOfRangeException(nameof(colorCode));
        }

        return $"\x03{colorCode}{source}\x03";
    }
}
