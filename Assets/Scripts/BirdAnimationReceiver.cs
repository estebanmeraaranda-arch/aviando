using UnityEngine;

public class BirdAnimationReceiver : MonoBehaviour
{
    private Animator anim;
    void Awake() { anim = GetComponent<Animator>(); }
    
    // Absorbe el evento de la animación original de Living Birds
    public void ResetHopInt()
    {
        if (anim != null) anim.SetInteger("hop", 0);
    }
}