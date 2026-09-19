# BDFrame的单元测试

## 目录

- [1.标签介绍：](#1标签介绍)
- [2.执行测试：](#2执行测试)
- [3.断言：](#3断言)
- [4.简单测试示例：](#4简单测试示例)

因为市面上的Xunit等单元测试库，无法在热更环境（ILRuntime时期，HyCLR可能会重整）下执行 ，BDFramework对框架本身进行编写了一套精简的单元测试系统，用以对框架本身进行测试。并可以扩展，以及对自己项目业务测试。

代码在BDFramework.UnitTest文件夹中

![](https://cdn.nlark.com/yuque/0/2020/png/338267/1602074149822-9d4fc115-8f7b-47b8-b48f-ea200352b24c.png)

#### 1.标签介绍：

![](https://cdn.nlark.com/yuque/0/2020/png/338267/1602074330882-0de0fd4d-a960-42f8-b599-d605e703413a.png)

UnitTestBaseAttribute为定义的热更标签的基类，如果自行扩展标签 ，也需要继承该类。

BDFrame为自己的业务测试，实现了：**UnitTestAttribute**、**HotfixUnitTestAttribute** 两个标签类

#### 2.执行测试：

![](https://cdn.nlark.com/yuque/0/2020/png/338267/1602074605130-9110cffa-37d4-4ff2-b23b-893bfa44bccc.png)

BDFrame中编写了这两个类，一个非热更使用，一个热更使用。

热更流程：

**I.搜集所有类数据**

![](https://cdn.nlark.com/yuque/0/2020/png/338267/1602074800858-72827278-1eb0-4b7e-a78e-6d4cd439008b.png)

以上两个类中均有实现该接口，比较通用，无需修改。

**II.执行测试**

![](https://cdn.nlark.com/yuque/0/2020/png/338267/1602074687600-da7dfad9-0f6d-43ea-a445-529df0ec3f47.png)

这里的T类型，为自定义标签，可以按自己扩展，并且调用执行。

注：注意热更 和非热更下情况不太一样，所以做了2个类的接口作区分。

#### 3.断言：

较为简单，就不介绍了。

**BDFramework.UnitTest.Assert** 类，跟大部分测试用例 用法保持一致。

#### 4.简单测试示例：

![](https://cdn.nlark.com/yuque/0/2020/png/338267/1602074909190-c9f2107e-1412-4d79-ae79-2ebc8dfb3063.png)
