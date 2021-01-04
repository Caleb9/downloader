using System;
using System.Linq;
using CSharpFunctionalExtensions;

namespace Api.Downloading
{
    public sealed class Link
    {
        private readonly Uri _value;

        private Link(
            string link)
        {
            _value = new Uri(link);
            if (_value.Scheme != "http" && _value.Scheme != "https")
            {
                throw new ArgumentException("Only http(s) links are supported.");
            }

            if (_value.Segments.Any() is false)
            {
                throw new ArgumentException($"{link} is not a file link.");
            }

            FileName = _value.Segments.Last();
        }

        public string FileName { get; }

        public string Url => _value.OriginalString;

        public static Result<Link> Create(
            string link)
        {
            try
            {
                return new Link(link);
            }
            catch (Exception e) when (e is UriFormatException || e is ArgumentException)
            {
                return Result.Failure<Link>(e.Message);
            }
        }

        public static implicit operator string(
            Link link)
        {
            return link._value.OriginalString;
        }
    }
}