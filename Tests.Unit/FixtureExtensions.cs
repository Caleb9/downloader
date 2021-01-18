using System.Text;
using AutoFixture;

namespace Tests.Unit
{
    internal static class FixtureExtensions
    {
        internal static string CreateLongString(this IFixture fixture)
        {
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < 1000; i++)
            {
                stringBuilder.Append(fixture.Create<string>());
            }

            return stringBuilder.ToString();
        }
    }
}