# Props:View渲染状态

## 目录

- [Props存储Window状态](#Props存储Window状态)

[demo地址](https://github.com/yimengfan/BDFramework.Core/tree/master/Assets/Code/Game%40hotfix/demo6_UFlux/01.Component "demo地址")

> 📌**Props** 本来是主要描述Window的**渲染**、**状态**数据，可以理解为渲染层的Data.
> （跟逻辑状态**State**要区别开，后面在Reducer中会讲解）.

# Props存储Window状态

如下面Props存储 当前页面的下标和点击次数

```c# 
//Props存储当前页面状态
public class Props_TestView : APropsBase
 {
     //当前页面下标
     public int  CurPageIdx;
     //当前点击次数
     public bool CurClickNum;
     //...
 }
 
//页面演示
 public TestWidow:Awindows<Props_TestView>
 { 
     [TranformPath("xxxx/Text")]
     private Text textClickNum;
 
    //刷新页面逻辑
     public void Refresh()
     {
        //根据当前页面
        if( this.props.CurPageIdx == 1)
        {
            this.textClickNum.text =  this.props.CurClickNum.tostring();
        }
     }
     
      //按钮点击影响
      [OnButtonClick("xxx/Button")]
      public void ClickTest()
      {
        this.props.CurClickNum++;
      }
     
 }

```
