using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


public class Prospector : MonoBehaviour
{

    static public Prospector S;

    [Header("Set in Inspector")]
    public TextAsset deckXML;
    public TextAsset layoutXML;
    public float xOffset = 3;
    public float yOffset = -2.5f;
    public Vector3 layoutCenter;
    public Vector2 fsPosMid = new Vector2(0.5f, 0.9f);
    public Vector2 fsPosRun = new Vector2(0.5f, 0.75f);
    public Vector2 fsPosMid2 = new Vector2(0.4f, 1.0f);
    public Vector2 fsPosEnd = new Vector2(0.5f, 0.95f);


    [Header("Set Dynamically")]
    public Deck deck;
    public Layout layout;
    public List<CardProspector> drawPile;
    public Transform layoutAnchor;
    public CardProspector target;
    public List<CardProspector> tableau;
    public List<CardProspector> discardPile;
    public FloatingScore fsRun;

    void Awake()
    {
        S = this;
    }

    void Start()
    {
        Scoreboard.S.score = ScoreManager.SCORE;

        deck = GetComponent<Deck>();
        deck.InitDeck(deckXML.text);
        Deck.Shuffle(ref deck.cards); // This shuffles the deck by reference

        //Card c;
        //for (int cNum = 0; cNum < deck.cards.Count; cNum++)
        //{
        //    c = deck.cards[cNum];
        //    c.transform.localPosition = new Vector3((cNum % 13) * 3, cNum / 13 * 4, 0);
        //}

        layout = GetComponent<Layout>();    // Get the Layout component
        layout.ReadLayout(layoutXML.text);  // Pass LayoutXML to it

        drawPile = ConvertListCardsToListCardProspectors(deck.cards);

        LayoutGame();
    }

    List<CardProspector> ConvertListCardsToListCardProspectors(List<Card> lCD)
    {
        List<CardProspector> lCP = new List<CardProspector>();
        CardProspector tCP;
        foreach (Card tCD in lCD)
        {
            tCP = tCD as CardProspector;
            lCP.Add(tCP);
        }
        return (lCP);
    }

    CardProspector Draw()
    {
        CardProspector cd = drawPile[0];
        drawPile.RemoveAt(0);
        return (cd);
    }

    void LayoutGame()
    {
        // Create an empty GameObject to serve as an anchor for the tableau
        if (layoutAnchor == null)
        {
            GameObject tGO = new GameObject("_LayoutAnchor");
            layoutAnchor = tGO.transform;
            layoutAnchor.transform.position = layoutCenter;
        }

        CardProspector cp;
        foreach (SlotDef tSD in layout.slotDefs)
        {
            cp = Draw();
            cp.faceUp = tSD.faceUp;
            cp.transform.parent = layoutAnchor;
            cp.transform.localPosition = new Vector3(layout.multiplier.x * tSD.x,
                                             layout.multiplier.y * tSD.y, -tSD.layerID);
            cp.layoutID = tSD.id;
            cp.slotDef = tSD;

            cp.state = eCardState.tableau;
            cp.SetSortingLayerName(tSD.layerName);

            tableau.Add(cp);
        }

        // Set which cards are hiding others
        foreach (CardProspector tCP in tableau)
        {
            foreach (int hid in tCP.slotDef.hiddenBy)
            {
                cp = FindCardByLayoutID(hid);
                tCP.hiddenBy.Add(cp);
            }
        }

        // Set up the initial target card
        MoveToTarget(Draw());

        // Set up the Draw pile
        UpdateDrawPile();
    }

    // Convert from the layoutID int to the CardProspector with that ID
    CardProspector FindCardByLayoutID(int layoutID)
    {
        foreach (CardProspector tCP in tableau)
        {
            // Search through all cards in the tableau List<>
            if (tCP.layoutID == layoutID)
            {
                // If the card has the same ID, return it
                return (tCP);
            }
        }
        // If it's not found, return null
        return (null);
    }

    // This turns cards in the Mine face-up or face-down
    void SetTableauFaces()
    {
        foreach (CardProspector cd in tableau)
        {
            bool faceUp = true;
            foreach (CardProspector cover in cd.hiddenBy)
            {
                if (cover.state == eCardState.tableau)
                {
                    faceUp = false;
                }
            }
            cd.faceUp = faceUp;
        }
    }

    void MoveToDiscard(CardProspector cd)
    {
        // Set the state of the card to discard
        cd.state = eCardState.discard;
        discardPile.Add(cd);                    // Add it to the discardPileList<>
        cd.transform.parent = layoutAnchor;     // Update its transform parent

        // Position this card on the discardPile
        cd.transform.localPosition = new Vector3(layout.multiplier.x * layout.discardPile.x,
             layout.multiplier.y * layout.discardPile.y, -layout.discardPile.layerID + 0.5f);
        cd.faceUp = true;
        // Place it on top of the pile for depth sorting
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(-100 + discardPile.Count);
    }

    // Make cd new target card
    void MoveToTarget(CardProspector cd)
    {
        if (target != null) MoveToDiscard(target);
        target = cd;
        cd.state = eCardState.target;
        cd.transform.parent = layoutAnchor;
        // Move to the target position
        cd.transform.localPosition = new Vector3(layout.multiplier.x * layout.discardPile.x,
            layout.multiplier.y * layout.discardPile.y, -layout.discardPile.layerID);

        cd.faceUp = true;
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(0);
    }

    // Arranges all the cards of the drawPile to show how many are left
    void UpdateDrawPile()
    {
        CardProspector cd;
        // Go through all the cards of the drawPile
        for (int i = 0; i < drawPile.Count; i++)
        {
            cd = drawPile[i];
            cd.transform.parent = layoutAnchor;

            // Position it correctly with the layout.drawPile.stagger
            Vector2 dpStagger = layout.drawPile.stagger;
            cd.transform.localPosition = new Vector3(layout.multiplier.x *
                (layout.drawPile.x + i * dpStagger.x), layout.multiplier.y *
                (layout.drawPile.y + i * dpStagger.y), -layout.drawPile.layerID + 0.1f * i);
            cd.faceUp = false;
            cd.state = eCardState.drawpile;
            // Set depth sorting
            cd.SetSortingLayerName(layout.drawPile.layerName);
            cd.SetSortOrder(-10 * i);
        }
    }

    public void CardClicked(CardProspector cd)
    {
        // This reaction is determined by the state of the clicked card
        switch (cd.state)
        {
            case eCardState.target:
                break;

            case eCardState.drawpile:
                MoveToDiscard(target);
                MoveToTarget(Draw());
                UpdateDrawPile();
                ScoreManager.EVENT(eScoreEvent.draw);
                FloatingScoreHandler(eScoreEvent.draw);
                break;

            case eCardState.tableau:
                bool validMatch = true;
                if (!cd.faceUp)
                {
                    validMatch = false;
                }
                if (!AdjacentRank(cd, target))
                {
                    validMatch = false;
                }
                if (!validMatch) return;

                tableau.Remove(cd);
                MoveToTarget(cd);
                SetTableauFaces();
                ScoreManager.EVENT(eScoreEvent.mine);
                FloatingScoreHandler(eScoreEvent.mine);
                break;
        }
        CheckForGameOver();
    }

    void CheckForGameOver()
    {
        if (tableau.Count == 0)
        {
            GameOver(true);
            return;
        }

        if (drawPile.Count > 0)
        {
            return;
        }

        // Check for remaining valid plays
        foreach (CardProspector cd in tableau)
        {
            if (AdjacentRank(cd, target))
            {
                return;
            }
        }

        GameOver(false);
    }

    void GameOver(bool won)
    {
        if (won)
        {
            //print("Game Over. You won!:)");
            ScoreManager.EVENT(eScoreEvent.gameWin);
            FloatingScoreHandler(eScoreEvent.gameWin);
        }
        else
        {
            //print("Game Over. You Lost. :(");
            ScoreManager.EVENT(eScoreEvent.gameLoss);
            FloatingScoreHandler(eScoreEvent.gameLoss);
        }

        // Reload the scene
        SceneManager.LoadScene("__Prospector_Scene_0");
    }

    public bool AdjacentRank(CardProspector c0, CardProspector c1)
    {
        // If either card is face-down, it's not adjacent.
        if (!c0.faceUp || !c1.faceUp) return (false);

        // If they are 1 apart, they are adjacent
        if (Mathf.Abs(c0.rank - c1.rank) == 1)
        {
            return (true);
        }

        if (c0.rank == 1 && c1.rank == 13) return (true);
        if (c0.rank == 13 && c1.rank == 1) return (true);

        return (false);
    }

    void FloatingScoreHandler(eScoreEvent evt)
    {
        List<Vector2> fsPts;
        switch (evt)
        {
            case eScoreEvent.draw:
            case eScoreEvent.gameWin:
            case eScoreEvent.gameLoss:
                // Add fsRun to the Scoreboard score
                if (fsRun != null)
                {
                    // Create points for the bezier curve
                    fsPts = new List<Vector2>();
                    fsPts.Add(fsPosRun);
                    fsPts.Add(fsPosMid2);
                    fsPts.Add(fsPosEnd);
                    fsRun.reportFinishTo = Scoreboard.S.gameObject;
                    fsRun.Init(fsPts, 0, 1);
                    fsRun.fontSizes = new List<float>(new float[] { 28, 36, 4 });
                    fsRun = null;
                }
                break;

            case eScoreEvent.mine:
                FloatingScore fs;
                //Move fs from the mousePosition to fsPosRun
                Vector2 p0 = Input.mousePosition;
                p0.x /= Screen.width;
                p0.y /= Screen.height;
                fsPts = new List<Vector2>();
                fsPts.Add(p0);
                fsPts.Add(fsPosMid);
                fsPts.Add(fsPosRun);
                fs = Scoreboard.S.CreateFloatingScore(ScoreManager.CHAIN, fsPts);
                fs.fontSizes = new List<float>(new float[] { 4, 50, 28 });
                if (fsRun == null)
                {
                    fsRun = fs;
                    fsRun.reportFinishTo = null;
                }
                else
                {
                    fs.reportFinishTo = fsRun.gameObject;
                }
                break;

        }
    }
}

