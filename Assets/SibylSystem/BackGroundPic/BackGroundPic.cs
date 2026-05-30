using UnityEngine;
using System;
public class BackGroundPic : Servant
{
    GameObject backGround;
    public override void initialize()
    {
        backGround = create(Program.I().mod_simple_ngui_background_texture, Vector3.zero, Vector3.zero, false, Program.ui_back_ground_2d);
        Texture2D pic = RuntimeTextureLoader.Load(RuntimeDirectory.Texture, "common", "desk.jpg");
        if (pic == null)
        {
            pic = RuntimeTextureLoader.Load(RuntimePaths.GetProjectRootFilePath("Assets", "Content", "Backgrounds", "desk.jpg"));
        }
        backGround.GetComponent<UITexture>().mainTexture = pic;
        backGround.GetComponent<UITexture>().depth = -100;
    }

    public override void applyShowArrangement()
    {
        UIRoot root = Program.ui_back_ground_2d.GetComponent<UIRoot>();
        float s = (float)root.activeHeight / Screen.height;
        var tex = backGround.GetComponent<UITexture>().mainTexture;
        float ss = (float)tex.height / (float)tex.width;
        int width = (int)(Screen.width * s);
        int height = (int)(width * ss);
        if (height < Screen.height)
        {
            height = (int)(Screen.height * s);
            width = (int)(height / ss);
        }
        backGround.GetComponent<UITexture>().height = height+2;
        backGround.GetComponent<UITexture>().width = width+2;
    }

    public override void applyHideArrangement()
    {
        applyShowArrangement();
    }
}
