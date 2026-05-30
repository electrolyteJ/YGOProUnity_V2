using UnityEngine;

public static class RuntimeTextureTools
{
    public static Texture2D Scale(Texture2D source, int targetWidth, int targetHeight)
    {
        Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, false);

        float incX = 1.0f / (float)targetWidth;
        float incY = 1.0f / (float)targetHeight;

        for (int i = 0; i < result.height; ++i)
        {
            for (int j = 0; j < result.width; ++j)
            {
                Color newColor = source.GetPixelBilinear((float)j / (float)result.width, (float)i / (float)result.height);
                result.SetPixel(j, i, newColor);
            }
        }

        result.Apply();
        return result;
    }

    public static Texture2D[] SliceField(Texture2D source)
    {
        Texture2D textureField = Scale(source, 1024, 819);
        Texture2D[] returnValue = new Texture2D[3];
        returnValue[0] = new Texture2D(textureField.width, textureField.height);
        returnValue[1] = new Texture2D(textureField.width, textureField.height);
        returnValue[2] = new Texture2D(textureField.width, textureField.height);
        float zuo = (float)textureField.width * 69f / 320f;
        float you = (float)textureField.width * 247f / 320f;
        for (int w = 0; w < textureField.width; w++)
        {
            for (int h = 0; h < textureField.height; h++)
            {
                Color c = textureField.GetPixel(w, h);
                if (c.a < 0.05f)
                {
                    c.a = 0;
                }
                if (w < zuo)
                {
                    returnValue[0].SetPixel(w, h, c);
                    returnValue[1].SetPixel(w, h, new Color(0, 0, 0, 0));
                    returnValue[2].SetPixel(w, h, new Color(0, 0, 0, 0));
                }
                else if (w > you)
                {
                    returnValue[2].SetPixel(w, h, c);
                    returnValue[0].SetPixel(w, h, new Color(0, 0, 0, 0));
                    returnValue[1].SetPixel(w, h, new Color(0, 0, 0, 0));
                }
                else
                {
                    returnValue[1].SetPixel(w, h, c);
                    returnValue[0].SetPixel(w, h, new Color(0, 0, 0, 0));
                    returnValue[2].SetPixel(w, h, new Color(0, 0, 0, 0));
                }
            }
        }
        returnValue[0].Apply();
        returnValue[1].Apply();
        returnValue[2].Apply();
        return returnValue;
    }
}
