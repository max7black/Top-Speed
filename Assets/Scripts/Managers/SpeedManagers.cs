using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class SpeedManagers : MonoBehaviour {
    public static float velocity = 0.0f;
    Text text;

    // Use this for initialization
    void Start () {
        velocity = UnityStandardAssets._2D.PlatformerCharacter2D.currentVelocity;
        text = GetComponent<Text>();
    }
	
	// Update is called once per frame
	void Update () {
        velocity = UnityStandardAssets._2D.PlatformerCharacter2D.currentVelocity;
        text.text = "Speed: " + velocity;
    }
}
