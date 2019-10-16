using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum eFSState
{
    idle,
    pre,
    active,
    post
}

public class FloatingScore : MonoBehaviour
{
    [Header("Set Dynamically")]
    public eFSState state = eFSState.idle;

    [SerializeField]
    protected int _score = 0;
    public string scoreString;

    public int score
    {
        get
        {
            return (_score);
        }
        set
        {
            _score = value;
            scoreString = _score.ToString("N0");
            GetComponent<Text>().text = scoreString;
        }
    }

    public List<Vector2> bezierPts;
    public List<float> fontSizes;
    public float timeStart = -1f;
    public float timeDuration = 1f;
    public string easingCurve = Easing.InOut;

    public GameObject reportFinishTo = null;

    private RectTransform rectTrans;
    private Text txt;

    // Set up the FloatingScore and movement
    public void Init(List<Vector2>ePts,float eTimeS = 0, float eTimeD = 1)
    {
        rectTrans = GetComponent<RectTransform>();
        rectTrans.anchoredPosition = Vector2.zero;

        txt = GetComponent<Text>();

        bezierPts = new List<Vector2>(ePts);

        if(ePts.Count == 1)
        {
            // There is only one point, go there
            transform.position = ePts[0];
            return;
        }

        // If eTimeS is the default, start at current time
        if (eTimeS == 0) eTimeS = Time.time;
        timeStart = eTimeS;
        timeDuration = eTimeD;

        state = eFSState.pre;
    }

    public void FSCallback(FloatingScore fs)
    {
        // When this callback is called by SendMessage, 
        // add the score from the calling FloatingScore
        score += fs.score;
    }

    // Update is called once per frame
    void Update()
    {
        if (state == eFSState.idle) return;

        float u = (Time.time - timeStart) / timeDuration;
        float uC = Easing.Ease(u, easingCurve);
        if(u<0)
        {
            state = eFSState.pre;
            txt.enabled = false;
        } else
        {
            if (u >= 1)
            { // we're done moving
                uC = 1;     // Set uC = 1 so we don't overshoot
                state = eFSState.post;
                if(reportFinishTo != null)
                {
                    // There is a callback GameObject
                    reportFinishTo.SendMessage("FSCallback", this);
                    Destroy(gameObject);
                } else
                {
                    // There is nothing to callback
                    state = eFSState.idle;
                }
            } else
            {
                // This is active and moving
                state = eFSState.active;
                txt.enabled = true;
            }
            // Use bezier curve to move this object
            Vector2 pos = Utils.Bezier(uC, bezierPts);
            rectTrans.anchorMin = rectTrans.anchorMax = pos;
            if(fontSizes != null && fontSizes.Count>0)
            {
                // Adjust the fontSize of this GUIText
                int size = Mathf.RoundToInt(Utils.Bezier(uC, fontSizes));
                GetComponent<Text>().fontSize = size;
            }
        }
    }
}
