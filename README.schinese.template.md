# Pickle：RimWorld 模组测试框架

为你的 RimWorld 模组写自动化测试，并在游戏里跑起来。Pickle 读取 Gherkin 场景，把它们放到一个运行中的会话上重演。测试点的是真实界面，等的是真实游戏状态，校验的是正在跑的模拟。不用 mock。

```
Scenario: a drafted colonist waits for combat
  Given the save "test-colony" is loaded
  Given a colonist "Soldier" exists
  When I draft "Soldier"
  And I wait 30 ticks
  Then "Soldier" is drafted
  And "Soldier" has job "Wait_Combat"
```

## 这是给谁用的

模组作者。Pickle 对普通游戏没有任何影响，所以除非你在做模组或测试模组，没有理由订阅它。

## 和 RimTest Redux 有什么区别

RimTest Redux 和其他 RimWorld 测试框架跑的是单元测试。你写 C# 测试方法，对着自己的代码断言，当你要验证的是一个函数时，这就是对的工具。

Pickle 高一层。一个场景加载存档，征召一个真实小人，点一个真实按钮，等真实的 tick，然后对着跑出来的殖民地断言。这能抓到单元测试看不见的 bug：加载失败的 def、互相冲突的补丁、在第 400 tick 卡住的 job driver。

两者互补。数学逻辑用单元测试，殖民地交给 Pickle。

## 怎么测试一个 RimWorld 模组

开发者模式下，Pickle 提供一个测试运行窗口。它列出所有带测试集的模组，运行你挑选的场景，并显示每一步和它的耗时。

某一步失败时，Pickle 会把游戏暂停在出错的状态上，让你直接看造成问题的那个殖民地。它同时抓取截图、日志尾部，以及每个小人的状态。

## 需要写 C# 吗

起步不需要。单独的 feature 文件不需要编译。Pickle 自带的步骤覆盖存档、世界设置、小人属性、手术、心情、工作、订单、区域、武器、衣物、警报、镜头和界面。等你需要它没覆盖的东西时，再用 C# 写自己的步骤定义。

## 能在 CI 里跑吗

可以。运行过程无需人看着，会输出 JUnit 文件、Cucumber 消息，以及一个可以直接从磁盘打开的报告页。失败的场景会变成拉取请求上的标注，而不是一行没人会去翻的日志。

你也可以从任意机器的浏览器里观察一次运行。没有窗口时，这是唯一能看到运行过程的方式。

## 需求

RimWorld 1.5、1.6 或 1.7，以及一个补丁库。Harmony 和 Concord 都可以。两个都有时 Pickle 会选 Concord；如果 Concord 接受了补丁却没真正生效，它会退回 Harmony。

## 如何开始

从模板仓库起步，或者给你已有的模组加一个 Pickle 文件夹。文档涵盖步骤、标签、fixture 和等待。

源码与文档：https://github.com/RimWorks/Rimworld-Pickle

## RimWorks 的其他模组开发工具

- [Quickstarts](https://steamcommunity.com/sharedfiles/filedetails/?id=3793646067)：从 quicktest 菜单直接进入一个配置好的殖民地。
- [RimLogging](https://steamcommunity.com/sharedfiles/filedetails/?id=3733484696)：结构化日志查看器，一键分享错误报告。
