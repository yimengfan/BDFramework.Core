using BDFramework.UFlux;
using BDFramework.UFlux.View.Props;
using BDFramework.UI;
using Game.demo6_UFlux;
using Game.demo6_UFlux._05.NodeHelper;
using UnityEngine;
using UnityEngine.UI;

[UI((int) WinEnum.Win_Demo6_Test003, "Windows/UFlux/demo003/Window_FluxTest003")]
public class Window_CustomLogicBind : AWindow<PropsDemo003Window>
{
    public Window_CustomLogicBind(string path) : base(path)
    {
    }

    [TransformPath("btn_ChangeChildNode")]
    private Button btn_ChangeChildNode;

    [TransformPath("btn_ChangeOneNode")]
    private Button btn_ChangeOneNode;

    [TransformPath("btn_ChangeNodeByInt")]
    private Button btn_ChangeNodeByInt;

    [TransformPath("btn_Close")]
    private Button btn_Close;
    public override void Init()
    {
        base.Init();
        btn_Close.onClick.AddListener(() =>
        {
            this.Close();
        });
        btn_ChangeChildNode.onClick.AddListener(() =>
        {
            int stars = Random.Range(1, 6);
            this.Props.StarItems.Clear();
            for (int i = 0; i < stars; i++)
            {
                var item = new PropsDemo003Item();
                item.EquipmentIconPath = "Image/1";
                item.EquipmentName     = "小新" + i + "号";
                item.SetAllPropertyChanged();
                this.Props.StarItems.Add(item);
            }

            this.Props.SetPropertyChange(nameof(this.Props.StarItems)); //设置属性更改
            this.SetProps();
        });

        btn_ChangeOneNode.onClick.AddListener(() =>
        {
            int i = Random.Range(1, 6);

            this.Props.OneNodeChange                   = new PropsDemo003Item();
            this.Props.OneNodeChange.EquipmentIconPath = "Image/"     + i;
            this.Props.OneNodeChange.EquipmentName     = "小新被刷新:" + i;
            this.Props.OneNodeChange.SetAllPropertyChanged();

            this.Props.SetPropertyChange(nameof(this.Props.OneNodeChange)); //设置属性更改

            this.SetProps();
        });

        btn_ChangeNodeByInt.onClick.AddListener(() =>
        {
            int count = Random.Range(1, 6);
            this.Props.value = count;

            this.Props.SetPropertyChange(nameof(this.Props.value)); //设置属性更改

            this.SetProps();
        });
    }
    
}