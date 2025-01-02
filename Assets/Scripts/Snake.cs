using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;

[RequireComponent(typeof(BoxCollider2D))]
public class Snake : MonoBehaviour
{
    public Transform segmentPrefab;
    public Vector2Int direction = Vector2Int.right;
    public float speed = 20f;
    public float speedMultiplier = 1f;
    public int initialSize = 4;
    public bool moveThroughWalls = false;

    private readonly List<Transform> segments = new List<Transform>();
    private Vector2Int input;
    private float nextUpdate;
    private SerialPort serialPort;

    private void Start()
    {
        try {
            serialPort = new SerialPort("/dev/cu.usbserial-140", 9600) {
                ReadTimeout = 50,
                WriteTimeout = 50,
                DtrEnable = true,
                RtsEnable = true
            };
            serialPort.Open();
        }
        catch (System.Exception e) {
            Debug.LogError($"Serial port error: {e.Message}");
        }
        ResetState();
    }

    private void Update()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            try {
                string data = serialPort.ReadLine().Trim();
                
                if (direction.x != 0f)
                {
                    if (data == "UP") input = Vector2Int.up;
                    else if (data == "DOWN") input = Vector2Int.down;
                }
                else if (direction.y != 0f)
                {
                    if (data == "RIGHT") input = Vector2Int.right;
                    else if (data == "LEFT") input = Vector2Int.left;
                }
            }
            catch { }
        }

        // 保留鍵盤控制作為備用
        if (direction.x != 0f)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) {
                input = Vector2Int.up;
            } else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) {
                input = Vector2Int.down;
            }
        }
        else if (direction.y != 0f)
        {
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) {
                input = Vector2Int.right;
            } else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) {
                input = Vector2Int.left;
            }
        }
    }

    private void ProcessArduinoInput(string data)
    {
        if (direction.x != 0f)
        {
            if (data == "UP") {
                input = Vector2Int.up;
            } else if (data == "DOWN") {
                input = Vector2Int.down;
            }
        }
        else if (direction.y != 0f)
        {
            if (data == "RIGHT") {
                input = Vector2Int.right;
            } else if (data == "LEFT") {
                input = Vector2Int.left;
            }
        }
    }

    private void FixedUpdate()
    {
        if (Time.time < nextUpdate) return;

        if (input != Vector2Int.zero) {
            direction = input;
        }

        for (int i = segments.Count - 1; i > 0; i--) {
            segments[i].position = segments[i - 1].position;
        }

        int x = Mathf.RoundToInt(transform.position.x) + direction.x;
        int y = Mathf.RoundToInt(transform.position.y) + direction.y;
        transform.position = new Vector2(x, y);

        nextUpdate = Time.time + (1f / (speed * speedMultiplier));
    }

    public void Grow()
    {
        Transform segment = Instantiate(segmentPrefab);
        segment.position = segments[segments.Count - 1].position;
        segments.Add(segment);
    
        if(serialPort != null && serialPort.IsOpen)
        {
            serialPort.Write("S"); // 發送加分信號
        }
    }

    public void ResetState()
    {
        direction = Vector2Int.right;
        transform.position = Vector3.zero;

        // Start at 1 to skip destroying the head
        for (int i = 1; i < segments.Count; i++) {
            Destroy(segments[i].gameObject);
        }

        // Clear the list but add back this as the head
        segments.Clear();
        segments.Add(transform);

        // -1 since the head is already in the list
        for (int i = 0; i < initialSize - 1; i++) {
            Grow();
        }
    }

    public bool Occupies(int x, int y)
    {
        foreach (Transform segment in segments)
        {
            if (Mathf.RoundToInt(segment.position.x) == x &&
                Mathf.RoundToInt(segment.position.y) == y) {
                return true;
            }
        }

        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Food"))
        {
            Grow();
        }
        else if (other.gameObject.CompareTag("Obstacle"))
        {
            ResetState();
        }
        else if (other.gameObject.CompareTag("Wall"))
        {
            if (moveThroughWalls) {
                Traverse(other.transform);
            } else {
                ResetState();
            }
        }
    }

    private void Traverse(Transform wall)
    {
        Vector3 position = transform.position;

        if (direction.x != 0f) {
            position.x = Mathf.RoundToInt(-wall.position.x + direction.x);
        } else if (direction.y != 0f) {
            position.y = Mathf.RoundToInt(-wall.position.y + direction.y);
        }

        transform.position = position;
    }
    private void OnDestroy()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
        }
    }

}
