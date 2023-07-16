using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.UI;

#region DATA_STRUCTURES
class Graph
{
    private Button[,] _buttons;
    private int _rows;
    private int _cols;

    public Graph(Button[,] buttons)
    {
        _buttons = buttons;
        _rows = buttons.GetLength(0);
        _cols = buttons.GetLength(1);
    }

    public List<Button> GetDFS(int row, int col)
    {
        List<Button> result = new List<Button>();
        bool[,] visited = new bool[_rows, _cols];
        DFS(row, col, visited, result);
        return result;
    }

    private void DFS(int row, int col, bool[,] visited, List<Button> result)
    {
        if (row < 0 || row >= _rows || col < 0 || col >= _cols || visited[row, col])
            return;

        Button button = _buttons[row, col];
        visited[row, col] = true;
        result.Add(button);

        // DFS in row and column directions
        DFS(row + 1, col, visited, result);
        DFS(row - 1, col, visited, result);
        DFS(row, col + 1, visited, result);
        DFS(row, col - 1, visited, result);

        // DFS diagonally
        DFS(row + 1, col + 1, visited, result);
        DFS(row - 1, col - 1, visited, result);
        DFS(row - 1, col + 1, visited, result);
        DFS(row + 1, col - 1, visited, result);
    }

    public List<Button> GetBFS(int row, int col, int mode)
    {
        List<Button> result = new List<Button>();
        bool[,] visited = new bool[_rows, _cols];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        visited[row, col] = true;
        queue.Enqueue(new Vector2Int(row, col));

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int currentRow = current.x;
            int currentCol = current.y;

            Button button = _buttons[currentRow, currentCol];
            result.Add(button);

            switch (mode)
            {
                // Check neighbors in row and column directions
                case 0:
                    CheckAndEnqueue(currentRow + 1, currentCol, visited, queue);//Vertical //Down
                    break;
                case 1:
                    CheckAndEnqueue(currentRow - 1, currentCol, visited, queue); //Up
                    break;


                case 2:
                    CheckAndEnqueue(currentRow, currentCol + 1, visited, queue);//Horizontal //Right
                    break;
                case 3:
                    CheckAndEnqueue(currentRow, currentCol - 1, visited, queue); //Left
                    break;



                // Check neighbors diagonally
                case 4:
                    CheckAndEnqueue(currentRow + 1, currentCol + 1, visited, queue);
                    break;
                case 5:
                    CheckAndEnqueue(currentRow - 1, currentCol - 1, visited, queue);
                    break;


                case 6:
                    CheckAndEnqueue(currentRow - 1, currentCol + 1, visited, queue);
                    break;
                case 7:
                    CheckAndEnqueue(currentRow + 1, currentCol - 1, visited, queue);
                    break;
            }
        }

        return result;
    }

    private void CheckAndEnqueue(int row, int col, bool[,] visited, Queue<Vector2Int> queue)
    {
        if (row >= 0 && row < _rows && col >= 0 && col < _cols && !visited[row, col])
        {
            visited[row, col] = true;
            queue.Enqueue(new Vector2Int(row, col));
        }
    }
}
#endregion

public class GridManager : MonoBehaviour
{
    [SerializeField] Button buttonPrefab;
    [SerializeField] Button[,] buttons;
    [SerializeField] GameManager _game_Manager;
    /*    [SerializeField] Stack<Button> nodes;*/
    public int current_move;
    [SerializeField] Queue<int> shapesStack = new Queue<int>();
    private Dictionary<int, Color> color;
    [SerializeField] IndicatorGrid _indicator_Grid;

    private void Start()
    {
        _game_Manager = FindObjectOfType<GameManager>();
        _indicator_Grid = FindObjectOfType<IndicatorGrid>();
        color = new Dictionary<int, Color>();

        color.Add(0, Color.green);
        color.Add(1, Color.blue);
        color.Add(2, Color.yellow);
        CreateGrid();
        EnemyAISpawn();
        Shapes();
    }

    void CreateGrid()
    {
        buttons = new Button[5, 4];

        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                Button button = Instantiate(buttonPrefab, transform);
                button.transform.SetParent(transform);
                button.transform.localScale = Vector3.one;

                buttons[row, col] = button;

                int buttonRow = row;
                int buttonCol = col;
                button.name = "BTN: " + buttonRow.ToString() + " " + buttonCol.ToString();
                //Player position
                int x = PlayerPrefs.GetInt("MySprites");
                if (buttonRow == 0 && buttonCol == 0)
                {
                    var m = buttons[buttonRow, buttonCol];
                    m.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;
                    m.image.color = color[_game_Manager.x];
                }
                button.onClick.AddListener(() =>
                {
                    //Do Something if the button is clicked here

                });
            }
        }
    }

    public void Shift(int r, int c, int r1, int c1) //This method is used to update the player sprite location.
    {
        if (buttons[r1, c1].transform.GetChild(0).GetComponent<SpriteRenderer>().enabled == false)
        {
            Color s_color = buttons[r, c].image.color;

            buttons[r, c].transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = false;
            buttons[r, c].image.color = Color.white;

            buttons[r1, c1].transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;
            buttons[r1, c1].image.color = s_color;

            _game_Manager.UpdatePlayerPosition(r1, c1);
        }
    }

    public void Fire(int r, int c, int d)
    {
        Graph graph = new Graph(buttons);
        if (Safe(r, c))
        {
            if (shapesStack.Count > 0)
            {
                current_move = shapesStack.Dequeue();
                ActivateColors(graph.GetBFS(r, c, current_move));
                _indicator_Grid.ChangeSignal(shapesStack);
            }
        }
    }

    private void ActivateColors(List<Button> nodes)
    {
        foreach (Button node in nodes)
        {
            if (/*!node.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled &&*/
                node.image.color != color[_game_Manager.x])
                node.image.color = Color.red;
        }
    }

    public void VanishColors()
    {
        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                if (buttons[row, col].image.color == Color.red)
                {
                    buttons[row, col].image.color = Color.white;
                }
            }
        }
    }

    private void Shapes()
    {
        _indicator_Grid.ChangeSignal(shapesStack);
        StartCoroutine(ShapePush(true));
    }

    private IEnumerator ShapePush(bool value)
    {
        while (value)
        {

            yield return new WaitForSeconds(2);
            int m = Random.Range(0, 8);
            shapesStack.Enqueue(m);
        }
    }

    #region ENEMY_ACTIVITY

    public void CheckEnemy()
    {
        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                if (buttons[row, col].image.color == Color.red)
                {
                    if (buttons[row, col].transform.GetChild(0).GetComponent<SpriteRenderer>().enabled)
                    {
                        _game_Manager._score++;
                        _game_Manager._score_Text.text = "Score : " + _game_Manager._score.ToString();
                        buttons[row, col].transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = false;
                    }
                }
            }
        }
    }

    private void EnemyAISpawn()
    {
        int row = Random.Range(0, 5);
        int col = Random.Range(0, 4);

        if (!buttons[row, col].transform.GetChild(0).GetComponent<SpriteRenderer>().enabled)
        {
            buttons[row, col].transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;
        }
    }
    #endregion

    #region CARDINAL_MATRIX_CHECK
    private bool Safe(int row, int col)
    {
        if ((row >= 0 && row <= 4) &&
            (col >= 0 && col <= 3))
        {
            return true;
        }
        else
        {
            return false;
        }
    } //Border Check
    public bool LeftSafe(int row, int col)
    {
        if (Safe(row, col - 1))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public bool RightSafe(int row, int col)
    {
        if (Safe(row, col + 1))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public bool UpSafe(int row, int col)
    {
        if (Safe(row - 1, col))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public bool DownSafe(int row, int col)
    {
        if (Safe(row + 1, col))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    #endregion
}
