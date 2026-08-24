# Yarn Match Polish Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 复现参考图风格的毛线消消乐选择区，完成底部优先递归收线、随机 4x8 网格、四方向解锁和定向隧道补位。

**Architecture:** 玩法由 `YarnMatchGame` 调度，`YarnMatchBoardModel`、`YarnMatchPoolModel`、`YarnMatchRackModel` 分别维护棋盘、选择区和收线台状态；`YarnMatchPresentation` 负责 UI 节点与动画，`YarnMatchVisualFactory` 负责程序化 Sprite，数据结构集中在 `YarnMatchTypes`。上方棋盘每列索引 0 是底部前端；下方是 4x8 网格，第一排初始可选，点击任意滚筒后将其上下左右邻居加入可选集合，解锁集合持续扩展而不是限制成单线路径。隧道附着在具体目标格上，带方向箭头和剩余数量；目标格取空后从已有队列沿方向补入。收线使用流程层调度、表现层执行的逐根协程：线团先在原位收紧，再飞入收线台，棋盘下落后重新扫描同色露出格。

**Tech Stack:** Unity 6 `6000.0.23f1c1`、C#、`UnityEngine.UI`、程序化 Sprite、对象池。

---

### Task 1: 修正棋盘收集方向

**Files:**
- Modify: `Assets/Scripts/YarnMatch/Core/YarnMatchGame.cs`

**Behavior:** 每列只能取 `stack[0]`；取走后剩余格子 Row 重新编号并向下补位；所有列当前底部露出的同色格都可被同一个滚筒连续收集，最多 3 个。

**Verification:** Unity 编译通过；Play Mode 点击滚筒后，底部格子消失、上方格子向下移动，顶部剩余数量减少。

### Task 2: 参考图风格与随机 4x8 选择区

**Files:**
- Modify: `Assets/Scripts/YarnMatch/Core/YarnMatchGame.cs`

**Behavior:** 所有用户可见文本改为中文；移除旧的大块面板和固定车道，改为深紫背景、淡紫圆角方格、立体彩色滚筒的参考图风格；下方使用随机颜色和位置的 4x8 网格；第一排全可选，四方向相邻位置持续解锁；隧道紧贴目标格并显示方向与剩余数量，隧道本身不可点击；点击滚筒后原槽位留空，延迟后沿隧道方向补出下一枚。

**Verification:** Game View 检查中文字体、无乱码、4x8 方格不重叠；检查第一排可选、邻居集合解锁、已取位置不平移；点击隧道目标后只消耗已有队列，不增加线团总数。

### Task 3: 道具与收线表现

**Files:**
- Modify: `Assets/Scripts/YarnMatch/Core/YarnMatchGame.cs`

**Behavior:** 增加每局一次的“刷新”按钮，仅重排未使用滚筒和隧道队列，不生成额外滚筒；增加滚筒从定向隧道出现的滑入动画；线团先原位缩小收拢，再沿毛线轨迹进入同色收线台，滚筒旋转受击并缓慢缩紧；每根线团完成后立即下落并递归扫描新的同色露出格，完成 3 根后脉冲清空。

**Verification:** Play Mode 点击刷新只改变下方未使用内容；点击同色滚筒能看到飞线、旋转和进度变化；胜负和重开流程仍可用。

### Task 4: Unity 实机验证

**Files:**
- Inspect: `Assets/Scripts/YarnMatch/Infrastructure/YarnMatchBootstrap.cs`
- Inspect: `Assets/Scripts/YarnMatch/Core/YarnMatchGame.cs`

**Verification:** 使用 Unity 6 导入并编译，检查 Console；进入 Play Mode 验证宽窗口和竖向参考布局，完成后停回编辑模式。
