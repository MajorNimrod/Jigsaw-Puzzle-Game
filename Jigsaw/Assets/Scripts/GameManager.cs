using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Game Elements")]
    [Range(2, 6)]
    [SerializeField] private int difficulty = 4;
    [Range(0, 5)]
    [SerializeField] private int snappingTolerance = 2;
    [SerializeField] private Transform gameHolder;
    [SerializeField] private Image background;
    [SerializeField] private Transform piecePrefab;

    [Header("Grid Lines")]
    [SerializeField] private GameObject lineRendererPrefab;
    private List<GameObject> innerGridLines = new();
    private Transform gridLineContainer;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private AudioClip snapClip;

    [Header("UI Elements")]
    [SerializeField] private List<Texture2D> imageTextures;
    [SerializeField] private Transform levelSelectPanel;
    [SerializeField] private Image levelSelectPrefab;
    [SerializeField] private GameObject playAgainButton;
    [SerializeField] private GameObject viewPictureButton;

    [Header("View Photo Elements")]
    [SerializeField] private GameObject photoPreviewPanel;
    [SerializeField] private RawImage photoPreviewImage;
    private Texture2D currentTexture;
    

    private List<Transform> pieces;
    private Vector2Int dimensions;
    private float width;
    private float height;

    private Transform draggingPiece = null;
    private Vector3 offset;

    private int piecesCorrect;
    void Start()
    {
        // Default UI reset
        photoPreviewPanel.SetActive(false);
        viewPictureButton.SetActive(false);

        // Create UI
        foreach(Texture2D texture in imageTextures)
        {
            Image image = Instantiate(levelSelectPrefab, levelSelectPanel);
            image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            // Assign button action
            image.GetComponent<Button>().onClick.AddListener(delegate { StartGame(texture); });
        }

        viewPictureButton.GetComponent<Button>().onClick.AddListener(displayPhotoPreview);
    }

    public void StartGame(Texture2D jigsawTexture)
    {
        // Hide UI
        levelSelectPanel.gameObject.SetActive(false);

        // Storing a list of the transform for each piece so we can track them later.
        pieces = new List<Transform>();

        // Calculate the size of each piece, based on difficulty.
        dimensions = GetDimensions(jigsawTexture, difficulty);

        // Create the pieces of the correct size with the correct textures.
        CreateJigsawPieces(jigsawTexture);

        // Place the pieces randomly into the visible area.
        Scatter();

        // Update the border to fit the chosen puzzle.
        UpdateBorder();

        // As we're starting the puzzle, there will be no correct pieces.
        piecesCorrect = 0;

        // updating the current texture stored in the data
        currentTexture = jigsawTexture;
        viewPictureButton.SetActive(true);
    }

    Vector2Int GetDimensions(Texture2D jigsawTexture, int difficulty)
    {
        Vector2Int dimensions = Vector2Int.zero;
        // Difficulty is the number of pieces on the smallest texture dimension.
        // This helps ensure the pieces are as square as possible.
        if (jigsawTexture.width < jigsawTexture.height)
        {
            dimensions.x = difficulty;
            dimensions.y = (difficulty * jigsawTexture.height) / jigsawTexture.width;
        } else
        {
            dimensions.x = (difficulty * jigsawTexture.width) / jigsawTexture.height;
            dimensions.y = difficulty;
        }
        
        return dimensions;
    }

    // Create all the jigsaw pieces
    void CreateJigsawPieces(Texture2D jigsawTexture)
    {
        // Both height & width are normalized to represent units.
        height = 1f / dimensions.y;
        float aspect = (float)jigsawTexture.width / jigsawTexture.height;
        width = aspect / dimensions.x;

        for (int row = 0; row < dimensions.y; row++)
        {
            for (int col = 0; col < dimensions.x; col++)
            {
                // Create the piece in the right location of the right size.
                Transform piece = Instantiate(piecePrefab, gameHolder);
                piece.localPosition = new Vector3(
                    (-width * dimensions.x / 2) + (width * col) + (width / 2),
                    (-height * dimensions.y / 2) + (height * row) + (height / 2),
                    -1);
                piece.localScale = new Vector3(width, height, 1f);

                // naming the GameObjects for the sake of debugging.
                piece.name = $"Piece {(row * dimensions.x) + col}";
                pieces.Add(piece);

                // Assign the correct part of the texture for this jigsaw piece.
                // We need width & height both to be normalized between 0 & 1 for the UV.
                float width1 = 1f / dimensions.x;
                float height1 = 1f / dimensions.y;
                // UV coord order is counter-clockwise: (0, 0), (1, 0), (0, 1), (1, 1)
                Vector2[] uv = new Vector2[4];
                uv[0] = new Vector2(width1 * col, height1 * row);
                uv[1] = new Vector2(width1 * (col + 1), height1 * row);
                uv[2] = new Vector2(width1 * col, height1 * (row + 1));
                uv[3] = new Vector2(width1 * (col + 1), height1 * (row + 1));
                // Assign our new UVs to mesh
                Mesh mesh = piece.GetComponent<MeshFilter>().mesh;
                mesh.uv = uv;
                // Update the texture on the piece
                piece.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", jigsawTexture);
            }
        }
    }

    // Place the pieces randomly in the visible area.
    private void Scatter()
    {
        // Calculate the visible orthographic size of the screen.
        float orthoHeight = Camera.main.orthographicSize;
        float screenAspect = (float)Screen.width / Screen.height;
        float orthoWidth = (screenAspect  * orthoHeight);

        // Ensure pieces are away from the edges.
        float pieceWidth = width * gameHolder.localScale.x;
        float pieceHeight = height * gameHolder.localScale.y;

        orthoHeight -= pieceWidth;
        orthoWidth -= pieceWidth;

        // Place each piece randomly in the visible area.
        foreach (Transform piece in pieces)
        {
            float x = Random.Range(-orthoWidth, orthoWidth);
            float y = Random.Range(-orthoHeight, orthoHeight);
            piece.position = new Vector3(x, y, -1);
        }
    }

    private void UpdateBorder()
    {
        LineRenderer lineRenderer = gameHolder.GetComponent<LineRenderer>();

        // Calculate half sizes to simplify code.
        float halfWidth = (width * dimensions.x) / 2f;
        float halfHeight = (height * dimensions.y) / 2f;

        // We want he border to be behind the pieces.
        float borderZ = 0f;

        // Set border verts, starting top left, going clockwise.
        lineRenderer.SetPosition(0, new Vector3(-halfWidth, halfHeight, borderZ));
        lineRenderer.SetPosition(1, new Vector3(halfWidth, halfHeight, borderZ));
        lineRenderer.SetPosition(2, new Vector3(halfWidth, -halfHeight, borderZ));
        lineRenderer.SetPosition(3, new Vector3(-halfWidth, -halfHeight, borderZ));

        // Set the thickness of the border line.
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;

        // Show the border line.
        lineRenderer.enabled = true;

        // Switch level selection background to a lower alpha with new overlay
        background.enabled = false;
        DrawInnerGridLines();
    }

    // Update is called once per frame.
    void Update()
    {
        // for escaping photo viewing.
        if (Input.GetKeyDown(KeyCode.Escape) && photoPreviewPanel.activeSelf)
        {
            photoPreviewPanel.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            AutoSolve();
        }

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit)
            {
                // Play the "pickup" sound immediately
                sfxSource.PlayOneShot(pickupClip);

                // Everything is moveable, so we don't need to check if it's a Piece.
                draggingPiece = hit.transform;
                offset = draggingPiece.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
                offset += Vector3.back;
            }
        }

        // When we release the mouse button stop dragging.
        if (draggingPiece && Input.GetMouseButtonUp(0))
        {
            SnapAndDisableIfCorrect();
            draggingPiece.position += Vector3.forward;
            draggingPiece = null;
        }

        // Set the dragged piece position to the position of the mouse.
        if (draggingPiece)
        {
            Vector3 newPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            //newPosition.z = draggingPiece.position.z;
            newPosition += offset;
            draggingPiece.position = newPosition;
        }
    }

    private void SnapAndDisableIfCorrect()
    {
        // We need to know the idx of the piece to determine it's correct position.
        int pieceIndex = pieces.IndexOf(draggingPiece);

        // The coordinates of the pieces in the puzzle.
        int col = pieceIndex % dimensions.x;
        int row = pieceIndex / dimensions.x;

        // The target position in the non-scaled coordinates.
        Vector2 targetPosition = new((-width * dimensions.x / 2) + (width * col) + (width / 2),
                                     (-height * dimensions.y / 2) + (height * row) + (height / 2));

        // Check if we're in the correct location.
        // snappingTolerance can be changed to adjust difficulty.
        if (Vector2.Distance(draggingPiece.localPosition, targetPosition) < (width / snappingTolerance))
        {
            // Snap to our destination.
            draggingPiece.localPosition = targetPosition;

            // play the "snap" sound
            sfxSource.PlayOneShot(snapClip);

            // Disable the collider so we can't click on the object anymore.
            draggingPiece.GetComponent<BoxCollider2D>().enabled = false;

            // Increase he number of correct pieces, and check for puzzle completion.
            piecesCorrect++;

            if (piecesCorrect == pieces.Count)
            {
                playAgainButton.SetActive(true);
            }
        }
    }

    public void RestartGame()
    {
        ClearOldGridLines();

        // Destroy all the puzzle pieces.
        foreach (Transform piece in pieces)
        {
            Destroy(piece.gameObject);
        }
        pieces.Clear();

        // Hide the outline.
        gameHolder.GetComponent<LineRenderer>().enabled = false;

        // Show the level select UI
        playAgainButton.SetActive(false);
        viewPictureButton.SetActive(false);
        levelSelectPanel.gameObject.SetActive(true);
    }

    private void ClearOldGridLines()
    {
        // Fully destroy every "LineRendererGrid(Clone)" under GameHolder
        foreach (Transform child in gameHolder)
        {
            if (child.name.Contains("LineRendererGrid"))
            {
                Destroy(child.gameObject);
            }
        }

        // Destroy the container if you made one
        if (gridLineContainer != null)
        {
            Destroy(gridLineContainer.gameObject);
            gridLineContainer = null;
        }

        innerGridLines.Clear();
    }

    private void DrawInnerGridLines()
    {
        if (gridLineContainer != null)
        {
            Destroy(gridLineContainer.gameObject);
        }

        // Create new empty GameObject under Game Holder to hold all lines
        GameObject containerObj = new GameObject("GridLines");
        gridLineContainer = containerObj.transform;
        gridLineContainer.SetParent(gameHolder);
        gridLineContainer.localPosition = Vector2.zero;
        gridLineContainer.localRotation = Quaternion.identity;
        gridLineContainer.localScale = Vector2.one;

        // Cleanup old ones
        foreach (var line in innerGridLines)
        {
            Destroy(line);
        }
        innerGridLines.Clear();

        float halfWidth = (width * dimensions.x) / 2f;
        float halfHeight = (height * dimensions.y) / 2f;

        // Vertical internal lines (columns - 1)
        for (int col = 1; col < dimensions.x; col++)
        {
            float x = -halfWidth + col * width;
            Vector2 start = new(x, -halfHeight);
            Vector2 end = new(x, halfHeight);
            innerGridLines.Add(CreateLine(start, end));
        }

        // Horizontal internal lines (rows - 1)
        for (int row = 1; row < dimensions.y; row++)
        {
            float y = -halfHeight + row * height;
            Vector2 start = new(-halfWidth, y);
            Vector2 end = new(halfWidth, y);
            innerGridLines.Add(CreateLine(start, end));
        }
    }

    private GameObject CreateLine(Vector2 start, Vector2 end)
    {
        GameObject lineObj = Instantiate(lineRendererPrefab, gameHolder);
        LineRenderer lr = lineObj.GetComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        return lineObj;
    }

    private void displayPhotoPreview()
    {
        // On button click
        if (photoPreviewPanel != null & photoPreviewImage != null && currentTexture != null)
        {
            bool isActive = photoPreviewPanel.activeSelf;

            if (!isActive)
            {
                photoPreviewPanel.SetActive(true);
                photoPreviewImage.texture = currentTexture;
            }
            else
            {
                photoPreviewPanel.SetActive(false);
            }
        }
    }

    public void ClosePhotoPreview()
    {
        photoPreviewPanel.SetActive(false);
    }

    public void AutoSolve()
    {
        if (pieces == null || pieces.Count == 0) return;

        for (int i = 0; i < pieces.Count; i++)
        {
            Transform piece = pieces[i];

            int col = i % dimensions.x;
            int row = i / dimensions.x;

            Vector2 targetPosition = new(
                (-width * dimensions.x / 2) + (width * col) + (width / 2),
                (-height * dimensions.y / 2) + (height * row) + (height / 2)
            );

            piece.localPosition = targetPosition;

            // Disable the collider
            var collider = piece.GetComponent<BoxCollider2D>();
            if (collider) collider.enabled = false;
        }

        // Set the correct piece count
        piecesCorrect = pieces.Count;

        // Reveal win UI
        playAgainButton.SetActive(true);
    }
}
