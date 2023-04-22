using System.Collections.Generic;

public static class EntStringParser
{
    public static List<BSPEnt> Parse(char[] entString)
    {
        var entities = new List<BSPEnt>();
        var tokenExtractor = new TokenExtractor(entString);

        while (tokenExtractor.TryGetToken(out var token)
               && token.Equals("{"))
        {
            entities.Add(ParseEntity(tokenExtractor));
        }

        return entities;
    }

    private static BSPEnt ParseEntity(TokenExtractor tokenExtractor)
    {
        var properties = new Dictionary<string, string>();
        
        while (tokenExtractor.TryGetToken(out var token)
               && !token.Equals("}"))
        {
            var key = token;
            if (!tokenExtractor.TryGetToken(out token)
                || token.Equals("}"))
            {
                break;
            }

            var value = token;

            properties.Add(key, value);
        }

        return new BSPEnt {strings = properties};
    }
}
