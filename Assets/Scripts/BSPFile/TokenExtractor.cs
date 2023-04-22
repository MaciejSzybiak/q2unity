using System.Collections.Generic;

public class TokenExtractor
{
    private readonly Queue<char> chars;
    private char current;

    public TokenExtractor(char[] text)
    {
        chars = new Queue<char>(text);
    }

    public bool TryGetToken(out string token)
    {
        if (chars.Count == 0)
        {
            token = null;
            return false;
        }

        current = chars.Dequeue();

        SkipGarbage();
        
        return TryReadQuotedToken(out token)
               || TryReadToken(out token);
    }

    private void SkipGarbage()
    {
        while ((current <= ' ' || IsComment)
               && chars.Count > 0)
        {
            SkipWhitespaces();
            SkipComments();
        }
    }

    private void SkipWhitespaces()
    {
        while (current <= ' '
               && chars.TryDequeue(out current))
        {
        }
    }

    private void SkipComments()
    {
        if (IsComment)
        {
            while (chars.TryDequeue(out current)
                   && !current.Equals('\n'))
            {
            }
        }
    }

    private bool TryReadQuotedToken(out string token)
    {
        token = string.Empty;
        if (current.Equals('"'))
        {
            while (chars.TryDequeue(out current) && !current.Equals('"'))
            {
                token += current;
            }

            return true;
        }

        return false;
    }

    private bool TryReadToken(out string token)
    {
        token = string.Empty;

        do
        {
            if (current <= 32)
            {
                break;
            }

            token += current;
        } while (chars.TryDequeue(out current));

        return true;
    }

    private bool IsComment => current.Equals('/')
                              && chars.TryPeek(out var next)
                              && next.Equals('/');
}