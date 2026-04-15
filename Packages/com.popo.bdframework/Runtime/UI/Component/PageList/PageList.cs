using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PageList : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    /// <summary>
    /// 滚动类型
    /// </summary>
    enum Direction
    {
        Horizontal, //水平方向
        Vertical //垂直方向
    }

    /// <summary>
    /// 滚动方向
    /// </summary>
    [SerializeField] Direction direction = Direction.Horizontal;

    private List<RectTransform> m_InstantiateItems = new List<RectTransform>();
    private List<RectTransform> m_oldItems = new List<RectTransform>();

    public System.Action<int, GameObject> onItemUpdateAction = null;
    public System.Action<int, GameObject> onItemRemoveAction = null;

    /// <summary>
    /// 不在显示区域多加载的数量(垂直滚动=>增加行 水平滚动=>增加列)
    /// </summary>
    [SerializeField, Range(4, 10)] private int m_BufferNo;

    /// <summary>
    /// 需要实例化的行列
    /// </summary>
    private Vector2 m_InstantiateSize = Vector2.zero;

    /// <summary>
    /// PageGrid数据
    /// </summary>
    private Vector2 m_Page;

    private Vector2 cellGap;

    private int dataCount;

    /// <summary>
    /// 
    /// </summary>
    private float m_PrevPos = 0;

    public Vector2 InstantiateSize
    {
        get
        {
            if (m_InstantiateSize == Vector2.zero)
            {
                float rows, cols;
                if (direction == Direction.Horizontal)
                {
                    rows = m_Page.x;
                    cols = m_Page.y + (float) m_BufferNo;
                }
                else
                {
                    rows = m_Page.x + (float) m_BufferNo;
                    cols = m_Page.y;
                }

                m_InstantiateSize = new Vector2(rows, cols);
            }

            return m_InstantiateSize;
        }
    }

    //一个item的宽高
    public Vector2 CellRect
    {
        get
        {
            return m_Cell != null
                ? new Vector2(cellGap.x + m_Cell.sizeDelta.x, cellGap.y + m_Cell.sizeDelta.y)
                : new Vector2(100, 100);
        }
    }

    private float scale
    {
        get { return direction == Direction.Horizontal ? 1f : -1f; }
    }

    public int PageScale
    {
        get { return direction == Direction.Horizontal ? (int) m_Page.x : (int) m_Page.y; }
    }

    public int PageCount
    {
        get { return (int) m_Page.x * (int) m_Page.y; }
    }

    public int InstantiateCount
    {
        get { return (int) InstantiateSize.x * (int) InstantiateSize.y; }
    }


    public float CellScale
    {
        get { return direction == Direction.Horizontal ? CellRect.x : CellRect.y; }
    }

    /// <summary>
    /// content
    /// </summary>
    public float DirectionPos
    {
        get { return direction == Direction.Horizontal ? m_Rect.anchoredPosition.x : m_Rect.anchoredPosition.y; }
    }

    /// <summary>
    /// content rtf
    /// </summary>
    private RectTransform m_Rect;

    private RectTransform viewPort;

    private void Awake()
    {
        viewPort = transform.Find("Viewport").GetComponent<RectTransform>();
        m_Rect = viewPort.Find("Content").GetComponent<RectTransform>();
        m_Rect.anchorMax = Vector2.up;
        m_Rect.anchorMin = Vector2.up;
        m_Rect.pivot = Vector2.up;
        m_Rect.anchoredPosition = new Vector2(0f, 0f);
        var _pageGrid = m_Rect.GetComponent<PageGrid>();
        m_Page = _pageGrid.Page;
        cellGap = _pageGrid.CellGap;
    }

    [SerializeField] private RectTransform m_Cell;

    private int m_CurrentIndex; //当前页显示区域的第一行（列）在整个conten中的位置

    /// <summary>
    /// 重置pagelist
    /// </summary>
    public void Reset()
    {
        for (int i = 0; i < m_InstantiateItems.Count; i++)
        {
            m_InstantiateItems[i].gameObject.SetActive(false);
            m_oldItems.Add(m_InstantiateItems[i]);
        }

        m_InstantiateItems.Clear();
        m_PrevPos = 0;
        m_CurrentIndex = 0;
        this.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0);
    }

    /// <summary>
    /// 由格子数量获取多少行多少列 水平计算行 垂直计算列
    /// </summary>
    /// <param name="num"></param>格子个数
    /// <returns></returns>
    private Vector2 GetRectByNum(int num)
    {
        return direction == Direction.Horizontal
            ? new Vector2(m_Page.x, Mathf.CeilToInt(num / m_Page.x))
            : new Vector2(Mathf.CeilToInt(num / m_Page.y), m_Page.y);
    }


    private void moveItemToIndex(int index, RectTransform item, bool onlyPos = false)
    {
        item.anchoredPosition = GetPosByIndex(index, item);
        if (!onlyPos)
            UpdateItem(index, item.gameObject);
    }

    /// <summary>
    /// 最大移动距离
    /// </summary>
    public float MaxPrevPos
    {
        get
        {
            float result;
            Vector2 max = GetRectByNum(dataCount);
            if (direction == Direction.Horizontal)
            {
                result = max.y * CellScale - viewPort.rect.width / 2;
            }
            else
            {
                result = max.x * CellScale - viewPort.rect.height / 2;
            }

            return result;
        }
    }

    private void CreateItem(int index)
    {
        RectTransform item = null;
        if (m_oldItems.Count > 0)
        {
            //从回收池中获得item
            item = m_oldItems[0];
            m_oldItems.Remove(item);
        }
        else
        {
            //创建新的item
            item = GameObject.Instantiate(m_Cell);
            item.SetParent(m_Rect, false);
            item.anchorMax = Vector2.up;
            item.anchorMin = Vector2.up;
            item.pivot = Vector2.up;
        }

        item.name = "item" + index;
        item.anchoredPosition = GetPosByIndex(index, item);
        m_InstantiateItems.Add(item);
        item.gameObject.SetActive(true);
        //updateItem(index, item.gameObject);
    }

    private void Start()
    {
//        this.Data(100, "UI/TpItem");
    }

    private void UpdateItem(int index, GameObject item)
    {
        item.SetActive(index < dataCount);
        if (item.activeSelf)
        {
            onItemUpdateAction(index, item);
        }
    }

    /// <summary>
    /// 设置content的大小
    /// </summary>
    /// <param name="rows"></param>行数
    /// <param name="cols"></param>列数
    private void SetBound(Vector2 bound)
    {
        m_Rect.sizeDelta = new Vector2(bound.y * CellRect.x, bound.x * CellRect.y);
    }

    private Vector2 GetPosByIndex(int index, RectTransform item)
    {
        float x, y;
        if (direction == Direction.Horizontal)
        {
            x = index % m_Page.x;
            y = Mathf.FloorToInt(index / m_Page.x);
        }
        else
        {
            x = Mathf.FloorToInt(index / m_Page.y);
            y = index % m_Page.y;
        }

        return new Vector2(y * CellRect.x, -x * CellRect.y);
    }

    PointerEventData mPointerEventData = null;

    void CacheDragPointerEventData(PointerEventData eventData)
    {
        if (mPointerEventData == null)
        {
            mPointerEventData = new PointerEventData(EventSystem.current);
        }

        mPointerEventData.button = eventData.button;
        mPointerEventData.position = eventData.position;
        mPointerEventData.pointerPressRaycast = eventData.pointerPressRaycast;
        mPointerEventData.pointerCurrentRaycast = eventData.pointerCurrentRaycast;
    }

    private string prefabName;

    // <summary>
    /// display
    /// </summary>
    /// <param name="data">Data.</param>
    public void Data(int dataCount, string prefabName)
    {
        Reset();
        this.dataCount = dataCount;
        this.prefabName = prefabName;
        //测试打开
        m_Cell = Resources.Load<RectTransform>(this.prefabName);

        //正式打开
//        m_Cell = BResources.Load<RectTransform>(this.prefabName);
        if (dataCount > PageCount)
        {
            SetBound(GetRectByNum(dataCount));
        }
        else
        {
            SetBound(m_Page);
        }

        //当数据的大小超过初始化预计大小
        if (dataCount > InstantiateCount)
        {
            while (m_InstantiateItems.Count < InstantiateCount)
            {
                CreateItem(m_InstantiateItems.Count);
            }
        }
        else
        {
            while (m_InstantiateItems.Count > dataCount)
            {
                RemoveItem(m_InstantiateItems.Count - 1);
            }

            while (m_InstantiateItems.Count < dataCount)
            {
                CreateItem(m_InstantiateItems.Count);
            }
        }

        if (dataCount > 0)
        {
            int count = Mathf.Min(m_InstantiateItems.Count, dataCount);
            for (int i = 0; i < count; i++)
            {
                UpdateItem(i, m_InstantiateItems[i].gameObject);
            }
        }
    }

    private void RemoveItem(int index)
    {
        RectTransform item = m_InstantiateItems[index];
        m_InstantiateItems.Remove(item);
        item.gameObject.SetActive(false);
        m_oldItems.Add(item);
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        CacheDragPointerEventData(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        //处理滚动边界
        float _offset = 0f;
        if (this.direction == Direction.Horizontal)
        {
            _offset = eventData.position.x - mPointerEventData.position.x + m_Rect.localPosition.x;
            _offset = Mathf.Min(Mathf.Max(_offset, -MaxPrevPos), -viewPort.rect.width / 2);
            m_Rect.localPosition = new Vector2(_offset, m_Rect.localPosition.y);
        }
        else
        {
            _offset = eventData.position.y - mPointerEventData.position.y + m_Rect.localPosition.y;
            _offset = Mathf.Max(Mathf.Min(_offset, MaxPrevPos), viewPort.rect.height / 2);
            m_Rect.localPosition = new Vector2(m_Rect.localPosition.x, _offset);
        }

        //处理移出显示区域的item到下面
        while (scale * DirectionPos - m_PrevPos < -CellScale * 2)
        {
            //偏移距离大于两个item时
            if (m_PrevPos <= -MaxPrevPos) return;

            m_PrevPos -= CellScale;

            List<RectTransform> range = m_InstantiateItems.GetRange(0, PageScale);
            m_InstantiateItems.RemoveRange(0, PageScale);
            m_InstantiateItems.AddRange(range);
            for (int i = 0; i < range.Count; i++)
            {
                moveItemToIndex(m_CurrentIndex * PageScale + m_InstantiateItems.Count + i, range[i]);
            }

            m_CurrentIndex++;
        }

        //处理移出显示区域的item到上面
        while (scale * DirectionPos - m_PrevPos > -CellScale)
        {
            //回移超过一个item
            if (Mathf.RoundToInt(m_PrevPos) >= 0) return;

            m_PrevPos += CellScale;

            m_CurrentIndex--;

            if (m_CurrentIndex < 0) return;

            List<RectTransform> range = m_InstantiateItems.GetRange(m_InstantiateItems.Count - PageScale, PageScale);
            m_InstantiateItems.RemoveRange(m_InstantiateItems.Count - PageScale, PageScale);
            m_InstantiateItems.InsertRange(0, range);
            for (int i = 0; i < range.Count; i++)
            {
                moveItemToIndex(m_CurrentIndex * PageScale + i, range[i]);
            }
        }
    }
}