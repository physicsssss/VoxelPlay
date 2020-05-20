using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoxelPlay;
public class SettingsMenu : MonoBehaviour
{
    VoxelPlayEnvironment env;
    public Slider renderDistanceSlider;
    // Start is called before the first frame update
    void Start()
    {
        env = VoxelPlayEnvironment.instance;
    }

    // Update is called once per frame
    void Update()
    {
      
    }
    public void ToggleFullScreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
    }
    public void SetResolution(float _height)
    {
        float _width = _height * (16f / 9f);
        print(_width);
        Screen.SetResolution((int)_width, (int)_height, Screen.fullScreen);
        
    }
    public void SetRenderDistance()
    {
        env.visibleChunksDistance = (int)renderDistanceSlider.value;
        
    }
}
