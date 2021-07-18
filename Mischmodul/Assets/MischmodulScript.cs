using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class MischmodulScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable[] buttons;
    public KMSelectable glitchButton;
    public SpriteRenderer[] sprites;
    public SpriteRenderer bgSprite;
    public Sprite[] allIcons;
    public Sprite[] glitches;
    public Sprite black;

    private Sprite chosenIcon;
    private Sprite[] displayedIcons = new Sprite[25];

    int? selected = null;
    int[] solution = Enumerable.Range(0, 25).ToArray();
    int[] grid = Enumerable.Range(0, 25).ToArray();
    string[] coords = new string[] { "A5", "B5", "C5", "D5", "E5", "A4", "B4", "C4", "D4", "E4", "A3", "B3", "C3", "D3", "E3", "A2", "B2", "C2", "D2", "E2", "A1", "B1", "C1", "D1", "E1" };

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    bool glitching;
    private Color[][] displayedPixels = new Color[25][];

    void Awake ()
    {
        moduleId = moduleIdCounter++;
        allIcons = allIcons.Where(x => x != null).ToArray();
        glitchButton.OnInteract += delegate () { StartCoroutine(GlitchEffect(0.5f)); return false; };
        GetComponent<KMBombModule>().OnActivate += delegate () { Activate(); };
        Bomb.OnBombExploded += delegate () 
        {
            if (!moduleSolved)
            {
                Debug.LogFormat("[Mischmodul #{0}] Bomb detonation detected. Upon termination, the module displayed the following grid:", moduleId);
                LogLetters(grid);
            }
        };
    }

    void Start ()
    {
        DetermineList();
        bgSprite.sprite = chosenIcon;
        GetTiles();
        SetTiles();
    }

    void Activate()
    {
        foreach (KMSelectable button in buttons)
        {
            button.OnInteract += delegate () { KeyPress(Array.IndexOf(buttons, button)); return false; };
            button.OnHighlight += delegate ()
            {
                if (!moduleSolved && !glitching)
                    sprites[Array.IndexOf(buttons, button)].sprite = black;
            };
            button.OnHighlightEnded += delegate () 
            {
                int ix = Array.IndexOf(buttons, button);
                if (!moduleSolved && !glitching && ix != selected)
                        sprites[ix].sprite = displayedIcons[grid[ix]];
            };
        }
        Audio.PlaySoundAtTransform("Intro", transform);
        grid.Shuffle();
        SetTiles();
        DoLogging();
    }

    void KeyPress(int pos)
    {
        if (moduleSolved) return;
        if (selected == null)
        {
            Audio.PlaySoundAtTransform("PistonOut", buttons[pos].transform);
            selected = pos;
            sprites[pos].sprite = black;
        }
        else if (pos == selected)
        {
            Audio.PlaySoundAtTransform("PistonIn", buttons[pos].transform);
            selected = null;
            sprites[pos].sprite = displayedIcons[grid[pos]];
        }
        else
        {
            Audio.PlaySoundAtTransform("PistonIn", buttons[pos].transform);
            int temp = grid[selected.Value];
            grid[selected.Value] = grid[pos];
            grid[pos] = temp;
            selected = null;
            SetTiles();
            StartCoroutine(CheckSolve());
        }
    }

    void DetermineList()
    {
        Sprite[] modsOnBomb = allIcons.Where(x => Bomb.GetModuleNames().Select(nm => nm.ToUpper())
                .Contains(x.name.ToUpper())).ToArray();
        if (UnityEngine.Random.Range(0, 2) == 0 && modsOnBomb.Length != 0)
            chosenIcon = modsOnBomb.PickRandom();
        else chosenIcon = allIcons.PickRandom();
        chosenIcon = allIcons.First(x => x.name.StartsWith("simon says", StringComparison.InvariantCultureIgnoreCase)); //DEBUG LINE
    }

    void GetTiles()
    {
        for (int i = 0; i < 25; i++)
        {
            displayedIcons[i] = Sprite.Create(chosenIcon.texture, new Rect((6 * (i % 5)) + 1, 6 * (i / 5) + 1, 6, 6), new Vector2(0.5f, 0.5f));
            displayedPixels[i] = displayedIcons[i].texture.GetPixels((int)displayedIcons[i].textureRect.x, (int)displayedIcons[i].textureRect.y, 6, 6);
        }
    }   
    void SetTiles()
    {
        for (int i = 0; i < 25; i++)
            sprites[i].sprite = displayedIcons[grid[i]];
    }
    
    void DoLogging()
    {
        Debug.LogFormat("[Mischmodul #{0}] The chosen module icon is {1}.", moduleId, chosenIcon.name.Replace('_', '’'));
        Debug.LogFormat("[Mischmodul #{0}] The generated grid is as follows:", moduleId);
        LogLetters(grid);
        Debug.LogFormat("[Mischmodul #{0}] (To solve the module, alphabetize the above list)", moduleId);
        Debug.LogFormat("[Mischmodul #{0}] If you feel this icon has too high a level of ambiguity, please contact Danny7007#1377 on Discord.", moduleId);
    }

    IEnumerator CheckSolve()
    {
        if (Enumerable.Range(0,25).All(x => IsCorrect(x)))
        {
            for (int i = 0; i < 25; i++)
                sprites[i].sprite = displayedIcons[i];
            moduleSolved = true;
            yield return new WaitForSeconds(0.1f);
            Audio.PlaySoundAtTransform("Solve", transform);
            yield return new WaitForSeconds(0.5f);
            GetComponent<KMBombModule>().HandlePass();
        }
        yield return null;
    }

    bool IsCorrect(int pos)
    {
        if (grid[pos] == solution[pos])
            return true;
        Color[] thisPixels = displayedPixels[grid[pos]];
        for (int i = 0; i < 36; i++)
            if (!thisPixels[i].Equals(displayedPixels[pos][i]))
                return false;
        return true;
    }


    IEnumerator GlitchEffect(float time)
    {
        if (glitching)
            yield break;
        glitching = true;
        selected = null;
        for (float elapsed = 0; elapsed < time; elapsed += 0.075f)
        {
            for (int i = 0; i < 25; i++)
                if (!IsCorrect(i))
                    sprites[i].sprite = glitches.PickRandom();
            yield return new WaitForSecondsRealtime(0.075f);
        }
        SetTiles();
        glitching = false;
    }

    void LogLetters(int[] input)
    {
        string output = string.Empty;
        for (int i = 0; i < 25; i++)
        {
            output += (char)(input[i] + 'A');
            if (i % 5 == 4)
            {
                Debug.LogFormat("[Mischmodul #{0}] {1}", moduleId, output);
                output = string.Empty;
            }
        }
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} swap A2 E3 to swap those coordinates. Commands can be chained with spaces. Use <!{0} test> to flicker the squares.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string input)
    {

        string Command = input.Trim().ToUpperInvariant();
        List<string> parameters = Command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (parameters[0] == "SWAP")
            parameters.RemoveAt(0);
        if (parameters.All(x => coords.Contains(x)))
        { 
            if ((parameters.Count % 2 == 0) ^ selected == null)
            {
                yield return "sendtochaterror All swaps need to be concluded.";
                yield break;
            }
            yield return null;
            foreach (string coord in parameters)
            {
                buttons[Array.IndexOf(coords, coord)].OnInteract();
                yield return new WaitForSeconds(0.2f);
            }
        }
        else if (Regex.IsMatch(Command, @"^\s*(test)|(flash)|(inspect)|(glitch)|(flicker)\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
        {
           yield return null;
           StartCoroutine(GlitchEffect(1.5f));
        }
    }

    IEnumerator TwitchHandleForcedSolve ()
    {
        //while (!moduleSolved)
        do
        {
            if (selected != null)
            {
                buttons[selected.Value].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            for (int i = 0; i < 25; i++)
            {
                if (grid[i] != i)
                {
                    buttons[i].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                    for (int j = i; j < 25; j++)
                    {
                        if (grid[j] == i)
                        {
                            buttons[j].OnInteract();
                            yield return new WaitForSeconds(0.001f);
                        }
                    }
                }
            }
        } while (false);
    }
}
