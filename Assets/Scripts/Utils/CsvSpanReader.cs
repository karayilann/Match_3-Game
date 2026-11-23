using System;

namespace Utils
{
    public static class CsvSpanReader
    {
        /// <summary>
        /// Slices the next non-empty line from a character span without allocating substrings, advancing the source span past line terminators.
        /// </summary>
        public static bool TryReadLine(ref ReadOnlySpan<char> remainingText, out ReadOnlySpan<char> line)
        {
            while (!remainingText.IsEmpty)
            {
                int lineEnd = 0;
                int length = remainingText.Length;

                while (lineEnd < length && remainingText[lineEnd] != '\n' && remainingText[lineEnd] != '\r')
                {
                    lineEnd++;
                }

                line = remainingText.Slice(0, lineEnd);

                int nextStart = lineEnd + 1;
                if (nextStart < length && remainingText[lineEnd] == '\r' && remainingText[nextStart] == '\n')
                {
                    nextStart++;
                }

                remainingText = nextStart <= length ? remainingText.Slice(nextStart) : ReadOnlySpan<char>.Empty;

                if (!line.IsEmpty)
                {
                    return true;
                }
            }

            line = ReadOnlySpan<char>.Empty;
            return false;
        }

        /// <summary>
        /// Reads the next comma-separated byte token from a line span and advances the span past the delimiter with zero heap allocation.
        /// </summary>
        public static bool TryReadByte(ref ReadOnlySpan<char> remainingLine, out byte result)
        {
            result = 0;

            if (remainingLine.IsEmpty)
            {
                return false;
            }

            int commaIndex = remainingLine.IndexOf(',');
            ReadOnlySpan<char> token;

            if (commaIndex < 0)
            {
                token = remainingLine;
                remainingLine = ReadOnlySpan<char>.Empty;
            }
            else
            {
                token = remainingLine.Slice(0, commaIndex);
                remainingLine = remainingLine.Slice(commaIndex + 1);
            }

            return TryParseByte(token, out result);
        }

        private static bool TryParseByte(ReadOnlySpan<char> span, out byte result)
        {
            result = 0;
            int start = 0;
            int end = span.Length - 1;

            while (start <= end && span[start] == ' ')
            {
                start++;
            }

            while (end >= start && span[end] == ' ')
            {
                end--;
            }

            if (start > end)
            {
                return false;
            }

            int value = 0;
            for (int i = start; i <= end; i++)
            {
                char c = span[i];
                if (c < '0' || c > '9')
                {
                    return false;
                }

                value = value * 10 + (c - '0');
                if (value > byte.MaxValue)
                {
                    return false;
                }
            }

            result = (byte)value;
            return true;
        }
    }
}

