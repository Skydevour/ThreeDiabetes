import { _decorator, Quat, Vec3, Prefab, UITransform, tween, ITweenOption, TweenEasing, Vec2, find, Component, Node, input, Input, EventKeyboard, KeyCode, Label, PolygonCollider2D, Collider2D, Contact2DType, UI, Animation, System, sys, instantiate, random, debug, Button, Layout, SpriteFrame } from 'cc';
import { AudioManager } from './AudioManager';
import { GameBtnControl } from './GameBtnControl';
import { GameInfoManager } from './GameInfoManager';
const { ccclass, property } = _decorator;
// 物块的状态值
var ICON_STATE_NORMAL = 1;// 正常时
var ICON_STATE_MOVE = 2;// 移动
var ICON_STATE_PRECANCEL = 3;// 准备取消
var ICON_STATE_PRECANCEL2 = 4;// 确定取消
var ICON_STATE_CANCEL = 5;// 取消中
var ICON_STATE_CANCELED = 6;// 取消了

// Global.js
var BOARD_ROW = 9;
var BOARD_COL = 10;
@ccclass('GamePlayControl')
export class GamePlayControl extends Component {

    private canvas: Node = null;

    @property(Node)
    board: Node = null;

    @property(Label)
    scoreLabel: Label = null;

    @property(Prefab)
    iconPrefab: Prefab = null;

    // 游戏参数
    row: number = 0;
    col: number = 0;
    typeNum: number = 0;
    isControl: boolean = false;
    chooseIconPos: Vec2;
    deltaPos: Vec2;
    iconsDataTable: Array<Array<any>> = new Array<Array<any>>();
    iconsTable: Array<Array<any>> = [];
    iconsAnimTable: Array<Array<Animation>> = [];
    iconsPosTable: Array<Array<Vec3>> = [];

    cancelNum: number = 0;
    moveNum: number = 0;

    nanchor: number = 1;

    // 预览相关状态
    isPreviewSwapped: boolean = false;  // 是否已经预览交换
    previewTargetPos: Vec2 = null;      // 预览目标位置
    lastAnchor: number = 0;              // 上一次的方向

    // 特殊元素生成相关已移除

    // 游戏初始化
    onLoad() {
        // console.log("onLoad!!!!!!!");
        this.canvas = find("Canvas");
        console.log(this.canvas);

        // // 初始化数据
        // this.initGameData();
        // // 初始化物块
        // this.initGameBoard();
        // 点击事件
        // this.canvas.on(Input.EventType.TOUCH_START, this.onmTouchBagan, this);
        // this.canvas.on(Input.EventType.TOUCH_MOVE, this.onmTouchMove, this);
        // this.canvas.on(Input.EventType.TOUCH_END, this.onmTouchEnd, this);
        input.on(Input.EventType.TOUCH_START, this.onmTouchBagan, this);
        input.on(Input.EventType.TOUCH_MOVE, this.onmTouchMove, this);
        input.on(Input.EventType.TOUCH_END, this.onmTouchEnd, this);

        globalThis.eventTarget.on("resetItemFunc", this.resetItemFunc, this)

    }
    start() {
        console.log("GamePlayControl:start!!!!!!!");
        this.reGameStart();
        // console.log((this.iconsTable[1][1]))
        // console.log(typeof(this.iconsTable[1][1]))
        // console.log(this.iconsTable[1][1].getComponent(Node));
        // console.log(this.iconsTable[1][1].Node);
        //  tween(this.iconsTable[1][1]).to(5, {position:new Vec3(200, 200)}).start();
        //this.iconsTable[1][1].setPosition(new Vec3(10, 10));
        //         let  quat : Quat = new Quat();
        // Quat.fromEuler(quat, 0, 90, 0);
        //         tween(this.iconsTable[1][1])
        //         .to(2.0, { 
        //             position: new Vec3(0, 10, 0),                   // 位置缓动
        //             scale: new Vec3(1.2, 3, 1),                     // 缩放缓动
        //             rotation:quat }                                 // 旋转缓动
        //         )                                   
        //         .start(); 
    }
    reGameStart() {
        // 初始化数据
        this.initGameData();
        // 初始化物块
        this.initGameBoard();
    }
    // 重新更新item
    resetItem() {
        //
        if (GameInfoManager.instance.resetNum > 0) {
            GameInfoManager.instance.resetNum--;
            this.resetItemFunc();
        }
    }
    // 更新棋盘，再来一次和重随要用到
    resetItemFunc() {
        let newiconsTable: Array<Array<any>> = [];
        let niconsPosTable: Array<Array<Vec3>> = [];
        var row = this.row;
        var col = this.col;
        var i, j;
        let totalnum = (row - 1) * (col - 1);
        let curtnum = 0;
        // 销毁结点
        for (i = 1; i < row; i++) {
            newiconsTable[i] = [];
            niconsPosTable[i] = []
            for (j = 1; j < col; j++) {
                newiconsTable[i][j] = this.iconsTable[i][j];
                niconsPosTable[i][j] = this.iconsPosTable[i][j];
                tween(newiconsTable[i][j]).to(1, { position: new Vec3(niconsPosTable[i][j].x, niconsPosTable[i][j].y - 750), scale: new Vec3(0.5, 0.5, 1) }).call(() => {
                    curtnum++;
                    // console.log("i,j="+curi+" "+curj)
                    // console.log("curtnum:"+curtnum)
                    // 计数，当达到最大就删除所有prefab
                    if (curtnum >= totalnum) {
                        for (let curi = 1; curi < row; curi++) {
                            for (let curj = 1; curj < col; curj++) {
                                newiconsTable[curi][curj].destroy();
                            }
                        }
                    }
                }).start();
            }
        }
        this.reGameStart();
    }


    update(deltaTime: number) {

    }
    // 设置动画
    setIconAnimObj(obj, name) {
        obj.play(name);// 播放name动画
    }
    // 用指定位置的item的动画组件 播放默认动画
    setIconNormalAnim(i, j) {
        this.setIconAnimObj(this.iconsAnimTable[i][j], "normal0" + this.iconsDataTable[i][j].iconType);
    }
    // 用iconsDataTable对象的obj属性（动画组件） 播放取消动画
    setIconCancelAnimObj(data) {
        //console.log("执行item的消除动画");
        this.setIconAnimObj(data.obj, "cancel0" + data.iconType);
    }
    // 用iconsDataTable对象的obj属性（动画组件） 播放默认动画
    setIconNormalAnimObj(data) {
        //console.log("执行item的默认动画");
        this.setIconAnimObj(data.obj, "normal0" + data.iconType);
    }
    // 对item进行设置状态
    setIconState(i, j, state) {
        /*
        1. 对指定位置的item设置状态
        2. 如果当前ij已经是确定取消状态了，并且要改回普通状态和取消状态就退出，一旦确定要取消状态，不能变
        3.  如果是取消状态，需要执行取消动画，并且重新生成item
        */
        if (this.iconsDataTable[i][j].state != state) {
            if ((this.iconsDataTable[i][j].state == ICON_STATE_PRECANCEL2) && (state == ICON_STATE_NORMAL || state == ICON_STATE_PRECANCEL)) {
                return;
            }
            // 设置状态
            this.iconsDataTable[i][j].state = state;
            var callBack = function () {
                // 解绑?
                this.iconsDataTable[i][j].obj.targetOff(this);
                // 设置已经取消了状态
                this.setIconState(i, j, ICON_STATE_CANCELED);
                // 要消除的数目
                this.cancelNum = this.cancelNum - 1;
                if (this.cancelNum == 0) {// 消除完了就重新生成补充回去
                    this.handelMessage("produce");
                }
            }
            // 执行取消状态
            if (state == ICON_STATE_CANCEL) {
                // 指定动画结束的回调事件
                this.iconsDataTable[i][j].obj.on('finished', callBack, this);
                // 执行取消动画
                this.setIconCancelAnimObj(this.iconsDataTable[i][j]);
            }
        }
    }
    // 主方法
    // 初始化数据
    initGameData() {
        this.row = 9;// 行
        this.col = 11; // 列
        this.typeNum = 6;// 方块数量
        this.isControl = false;// 是否控制着小方块
        this.chooseIconPos = new Vec2(-1, -1);// 控制小方块的位置
        this.deltaPos = new Vec2(0, 0);// 相差坐标
        // this.score = 0;// 分数
        // 每个icon位置的数据信息，存储着对象信息
        this.iconsDataTable = [];
        for (var i = 1; i < this.row; i++) {
            this.iconsDataTable[i] = [];// 清空
            for (var j = 1; j < this.col; j++) {
                // state状态，icontype类型，obj物体的动画对象组件
                this.iconsDataTable[i][j] = { "state": ICON_STATE_NORMAL, "iconType": 1, "obj": null };
                // 类型随机获取
                this.iconsDataTable[i][j].iconType = this.getNewIconType(i, j);
            }
        }
    }
    // 初始化面板显示
    initGameBoard() {
        this.iconsTable = [];// 存储prefab实体
        this.iconsAnimTable = [];// 每个item的动画组件对象
        this.iconsPosTable = [];// 每个item的位置对象
        var row = this.row;
        var col = this.col;
        var i, j;
        // 初始化棋盘
        for (i = 1; i < row; i++) {
            this.iconsTable[i] = [];
            this.iconsPosTable[i] = [];
            this.iconsAnimTable[i] = [];
            for (j = 1; j < col; j++) {
                // 实例化
                var item = instantiate(this.iconPrefab);
                this.iconsTable[i][j] = item;// 赋值
                // 设置动画
                this.iconsAnimTable[i][j] = item.getComponent(Animation);
                // 添加到面板上
                item.parent = this.board;
                this.board.addChild(item);

                // 赋给全局变量
                this.iconsDataTable[i][j].obj = this.iconsAnimTable[i][j];
                this.setIconNormalAnim(i, j);// 给这个item设置默认动画

                // 设置随机坐标
                let range1 = 360 + 360;
                let rx = Math.floor(Math.random() * (range1)) - 360;
                let range2 = 640 + 640;
                let ry = Math.floor(Math.random() * (range2)) - 640;

                // 一开始设置随机坐标
                item.setPosition(rx, ry);

                // 设置坐标
                var x = -320 + 71 * i;
                var y = -360 + 71 * j;
                this.iconsPosTable[i][j] = new Vec3(x, y);// 保存起来
                // item.setPosition(x, y);//设置坐标

                // 然后动画移动过去
                tween(item).to(1.0, { position: new Vec3(x, y) }, { easing: "quadIn" }).start();
            }
        }
    }

    // 触摸方法
    onmTouchBagan(event) {
        console.log("GameInfoManager.instance.isStop:" + GameInfoManager.instance.isStop);
        if (GameInfoManager.instance.isStop || GameInfoManager.instance.isOver) {
            return;
        }
        console.log("onmTouchBagan(event)");

        // 重置预览状态
        if (this.isPreviewSwapped) {
            this.restorePreviewSwap();
        }
        this.isPreviewSwapped = false;
        this.previewTargetPos = null;
        this.lastAnchor = 0;
        var touches = event.getTouches();
        console.log("11");
        console.log(touches[0]);
        // var touchLoc = touches[0].getLocation();
        var touchLoc = touches[0].getUILocation();
        console.log("选择触碰位置：x:" + touchLoc.x + ",y:" + touchLoc.y);
        // console.log("选择触碰位置：x:"+touchLoc2.x+",y:"+touchLoc2.y);
        //console.log("this.iconsTable[i][j].getComponent(UITransform).getBoundingBoxToWorld():"+this.iconsTable[1][1].getComponent(UITransform).getBoundingBoxToWorld())
        //console.log("this.iconsTable[i][j].getComponent(UITransform).getBoundingBox():"+this.iconsTable[1][1].getComponent(UITransform).getBoundingBox())
        for (var i = 1; i < this.row; i++) {
            for (var j = 1; j < this.col; j++) {
                // 获取包围盒到世界坐标？就是检测是否选中一个item
                if (this.iconsTable[i][j].getComponent(UITransform).getBoundingBoxToWorld().contains(touchLoc)) {
                    console.log("this.iconsTable[i][j].getComponent(UITransform).getBoundingBoxToWorld().contains(touchLoc)");
                    this.isControl = true;
                    // 保存选中的坐标ij
                    this.chooseIconPos.x = i; // 行
                    this.chooseIconPos.y = j;// 列
                    // 这个不知道什么
                    this.deltaPos.x = this.iconsTable[i][j].getPosition().x - touchLoc.x;
                    this.deltaPos.y = this.iconsTable[i][j].getPosition().y - touchLoc.y;
                    // this.iconsTable[i][j].zIndex = -1;// 最上层
                    this.iconsTable[i][j].setSiblingIndex(-1);
                    break;
                }
            }
        }
    }
    onmTouchMove(event) {
        console.log("GameInfoManager.instance.isStop:" + GameInfoManager.instance.isStop);
        if (GameInfoManager.instance.isStop || GameInfoManager.instance.isOver) {
            return;
        }
        console.log("onmTouchMove(event)");
        // 是否有选中
        if (this.isControl) {

            var touches = event.getTouches();// 
            //var touchLoc = touches[0].getLocation();// 获取触点位置对象，xy
            var touchLoc = touches[0].getUILocation();
            // var startTouchLoc = touches[0].getStartLocation();// 获取基于touchLoc的上一次点击的触点位置对象，xy
            var startTouchLoc = touches[0].getStartLocation();
            console.log("Move-这次触碰位置：x:" + touchLoc.x + ",y:" + touchLoc.y);
            console.log("Move-开始触碰位置：x:" + startTouchLoc.x + ",y:" + startTouchLoc.y);
            // 计算移动的距离（相对于开始位置）
            var deltaX = touchLoc.x - startTouchLoc.x;
            var deltaY = touchLoc.y - startTouchLoc.y;
            var deltaX2 = deltaX * deltaX;
            var deltaY2 = deltaY * deltaY;
            var deltaDistance = deltaX2 + deltaY2;
            var anchor = 1;// 方向
            // 获取点击方向 - 使用改进的方向判断逻辑
            const directionThreshold = 0.6;
            const absX = Math.abs(deltaX);
            const absY = Math.abs(deltaY);

            if (absX > absY * directionThreshold) {
                // 水平移动占主导
                if (deltaX < 0) {
                    console.log("向左边滑动");
                    anchor = 1;// 往左边滑动
                } else {
                    console.log("往右边滑动");
                    anchor = 3;// 往右边滑动
                }
            } else if (absY > absX * directionThreshold) {
                // 垂直移动占主导
                if (deltaY > 0) {
                    console.log("往上边滑动");
                    anchor = 2;// 往上边滑动
                } else {
                    console.log("往下边滑动");
                    anchor = 4;// 往下边滑动
                }
            } else {
                // 方向不明确，使用备用逻辑
                if (this.lastAnchor != 0) {
                    anchor = this.lastAnchor;
                } else {
                    // 默认为水平方向
                    if (deltaX < 0) {
                        anchor = 1;
                    } else {
                        anchor = 3;
                    }
                }
            }
            this.nanchor = anchor;
            // 判断拖动区域是否出界
            if (this.chooseIconPos.x == 1 && anchor == 1) {// 往左边滑动
                console.log("向左边滑动->出界");
                // 是出界就恢复原来位置
                this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y].setPosition(this.iconsPosTable[this.chooseIconPos.x][this.chooseIconPos.y]);;
                // this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y].zIndex = 0;
                this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y].setSiblingIndex(0);
                this.isControl = false;
                return;
            } else if (this.chooseIconPos.x == this.row - 1 && anchor == 3) {// 往右边滑动
                console.log("向右边滑动->出界");
                // 是出界就恢复原来位置
                this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y].setPosition(this.iconsPosTable[this.chooseIconPos.x][this.chooseIconPos.y]);;
                // this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y].zIndex = 0;
                this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y].setSiblingIndex(0);
                this.isControl = false;
                return;
            } else if (this.chooseIconPos.y == this.col - 1 && anchor == 2) {// 往上边滑动
                console.log("向上边滑动->出界");
                // 是出界就恢复原来位置
                this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y].setPosition(this.iconsPosTable[this.chooseIconPos.x][this.chooseIconPos.y]);;
                // this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y].zIndex = 0;
                this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y].setSiblingIndex(0);
                this.isControl = false;
                return;
            } else if (this.chooseIconPos.y == 1 && anchor == 4) {// 往下边滑动
                console.log("向下边滑动->出界");
                // 是出界就恢复原来位置
                this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y].setPosition(this.iconsPosTable[this.chooseIconPos.x][this.chooseIconPos.y]);;
                // this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y].zIndex = 0;
                this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y].setSiblingIndex(0);
                this.isControl = false;
                return;
            }

            // 根据移动方向，让元素向指定方向移动最多一格的距离
            if (deltaDistance > 1600) {// 40*40像素，降低触发阈值
                // 检查方向是否改变，如果改变则先恢复之前的预览
                if (this.isPreviewSwapped && this.lastAnchor != anchor) {
                    this.restorePreviewSwap();
                }

                // 对于所有方向，都显示预览交换效果
                this.showPreviewSwap(anchor);

                this.lastAnchor = anchor;
            } else {
                // 移动距离不够，先恢复任何预览状态
                if (this.isPreviewSwapped) {
                    this.restorePreviewSwap();
                }

                // 根据移动方向按比例移动（最多移动一格距离）
                var originalPos = this.iconsPosTable[this.chooseIconPos.x][this.chooseIconPos.y];
                var moveRatio = Math.min(deltaDistance / 1600, 1.0); // 移动比例，最大为1
                var newPos = new Vec3(originalPos.x, originalPos.y, originalPos.z);

                if (anchor == 1) {// 左
                    var targetX = this.iconsPosTable[this.chooseIconPos.x - 1][this.chooseIconPos.y].x;
                    newPos.x = originalPos.x + (targetX - originalPos.x) * moveRatio;
                } else if (anchor == 2) {// 上
                    var targetY = this.iconsPosTable[this.chooseIconPos.x][this.chooseIconPos.y + 1].y;
                    newPos.y = originalPos.y + (targetY - originalPos.y) * moveRatio;
                } else if (anchor == 3) {// 右
                    var targetX = this.iconsPosTable[this.chooseIconPos.x + 1][this.chooseIconPos.y].x;
                    newPos.x = originalPos.x + (targetX - originalPos.x) * moveRatio;
                } else if (anchor == 4) {// 下
                    var targetY = this.iconsPosTable[this.chooseIconPos.x][this.chooseIconPos.y - 1].y;
                    newPos.y = originalPos.y + (targetY - originalPos.y) * moveRatio;
                }

                // 设置元素位置
                this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y].setPosition(newPos);
                this.lastAnchor = 0; // 重置方向记录
            }
        }
    }
    // 触摸结束
    onmTouchEnd(event) {
        console.log("GameInfoManager.instance.isStop:" + GameInfoManager.instance.isStop);
        if (GameInfoManager.instance.isStop || GameInfoManager.instance.isOver) {
            return;
        }
        console.log("onmTouchEnd(event)");
        if (this.isControl) {
            var touches = event.getTouches();// 
            //var touchLoc = touches[0].getLocation();// 获取触点位置对象，xy
            var touchLoc = touches[0].getUILocation();
            var startTouchLoc = touches[0].getStartLocation();// 获取基于touchLoc的上一次点击的触点位置对象，xy
            // var startTouchLoc = touches[1].getUIPreviousLocation();
            // 
            console.log("End-这次触碰位置：x:" + touchLoc.x + ",y:" + touchLoc.y);
            console.log("End-开始触碰位置：x:" + startTouchLoc.x + ",y:" + startTouchLoc.y);
            // 计算移动的距离
            var deltaX = touchLoc.x - startTouchLoc.x;
            var deltaY = touchLoc.y - startTouchLoc.y;
            var deltaX2 = deltaX * deltaX;
            var deltaY2 = deltaY * deltaY;
            var deltaDistance = deltaX2 + deltaY2;
            var anchor = 1;// 方向

            // 重置控制状态
            this.isControl = false;

            // 如果有预览交换，执行最后一次交换逻辑
            if (this.isPreviewSwapped && this.lastAnchor > 0) {
                console.log("触摸结束：执行最后一次预览交换，方向:" + this.lastAnchor);
                // 先快速恢复预览状态（无动画）
                this.restorePreviewSwapInstant();
                // 执行实际交换
                this.handelMessage("exchange", { "pos": touchLoc, "anchor": this.lastAnchor });
            } else {
                // 没有预览交换，检查是否有足够的移动距离来触发交换
                if (deltaDistance > 1600) {// 40*40像素，与移动阈值保持一致
                    // 获取点击方向
                    if (deltaX2 > deltaY2) {
                        if (deltaX < 0) {
                            console.log("向左边滑动");
                            anchor = 1;// 往左边滑动
                        } else {
                            console.log("往右边滑动");
                            anchor = 3;// 往右边滑动
                        }
                    } else {
                        if (deltaY > 0) {
                            console.log("往上边滑动");
                            anchor = 2;// 往上边滑动
                        } else {
                            console.log("往下边滑动");
                            anchor = 4;// 往下边滑动
                        }
                    }

                    // 检查是否出界
                    var canExchange = true;
                    if (this.chooseIconPos.x == 1 && anchor == 1) {// 往左边滑动
                        canExchange = false;
                    } else if (this.chooseIconPos.x == this.row - 1 && anchor == 3) {// 往右边滑动
                        canExchange = false;
                    } else if (this.chooseIconPos.y == this.col - 1 && anchor == 2) {// 往上边滑动
                        canExchange = false;
                    } else if (this.chooseIconPos.y == 1 && anchor == 4) {// 往下边滑动
                        canExchange = false;
                    }

                    if (canExchange) {
                        // 这是发送消息，交换，触碰坐标和方向
                        console.log("触摸结束：执行交换");
                        this.handelMessage("exchange", { "pos": touchLoc, "anchor": anchor });
                    } else {
                        console.log("触摸结束：交换出界，不执行交换");
                        // 恢复元素位置
                        this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y].setPosition(this.iconsPosTable[this.chooseIconPos.x][this.chooseIconPos.y]);
                        this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y].setSiblingIndex(0);
                    }
                } else {
                    console.log("触摸结束：移动距离不够，不执行交换");
                    // 恢复元素位置
                    this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y].setPosition(this.iconsPosTable[this.chooseIconPos.x][this.chooseIconPos.y]);
                    this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y].setSiblingIndex(0);
                }
            }

            // 重置预览状态
            this.isPreviewSwapped = false;
            this.previewTargetPos = null;
            this.lastAnchor = 0;
        }
    }

    // 显示预览交换效果
    showPreviewSwap(anchor) {
        if (this.isPreviewSwapped) {
            return; // 已经在预览状态，不重复执行
        }

        var targetX, targetY;
        if (anchor == 1) { // 左
            targetX = this.chooseIconPos.x - 1;
            targetY = this.chooseIconPos.y;
        } else if (anchor == 2) { // 上
            targetX = this.chooseIconPos.x;
            targetY = this.chooseIconPos.y + 1;
        } else if (anchor == 3) { // 右
            targetX = this.chooseIconPos.x + 1;
            targetY = this.chooseIconPos.y;
        } else if (anchor == 4) { // 下
            targetX = this.chooseIconPos.x;
            targetY = this.chooseIconPos.y - 1;
        } else {
            return; // 无效方向
        }

        // 记录预览目标位置
        this.previewTargetPos = new Vec2(targetX, targetY);

        // 交换两个元素的位置（视觉效果）
        var currentPos = this.iconsPosTable[this.chooseIconPos.x][this.chooseIconPos.y];
        var targetPos = this.iconsPosTable[targetX][targetY];

        this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y].setPosition(targetPos);
        this.iconsTable[targetX][targetY].setPosition(currentPos);

        this.isPreviewSwapped = true;
        console.log("显示预览交换：当前(" + this.chooseIconPos.x + "," + this.chooseIconPos.y + ") <-> 目标(" + targetX + "," + targetY + ")");
    }

    // 恢复预览交换（带动画）
    restorePreviewSwap() {
        if (!this.isPreviewSwapped || !this.previewTargetPos) {
            return;
        }

        // 恢复两个元素的原始位置
        var currentPos = this.iconsPosTable[this.chooseIconPos.x][this.chooseIconPos.y];
        var targetPos = this.iconsPosTable[this.previewTargetPos.x][this.previewTargetPos.y];

        this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y].setPosition(currentPos);
        this.iconsTable[this.previewTargetPos.x][this.previewTargetPos.y].setPosition(targetPos);

        this.isPreviewSwapped = false;
        console.log("恢复预览交换");
    }

    // 快速恢复预览交换（无动画）
    restorePreviewSwapInstant() {
        if (!this.isPreviewSwapped || !this.previewTargetPos) {
            return;
        }

        // 快速恢复两个元素的原始位置
        var currentPos = this.iconsPosTable[this.chooseIconPos.x][this.chooseIconPos.y];
        var targetPos = this.iconsPosTable[this.previewTargetPos.x][this.previewTargetPos.y];

        this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y].setPosition(currentPos);
        this.iconsTable[this.previewTargetPos.x][this.previewTargetPos.y].setPosition(targetPos);

        // 重置层级
        this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y].setSiblingIndex(0);
        this.iconsTable[this.previewTargetPos.x][this.previewTargetPos.y].setSiblingIndex(0);

        console.log("快速恢复预览交换");
    }

    // 消息函数
    handelMessage(message, data = null) {
        // 交换
        if (message == "exchange") {
            // 注意这里：交换效果前，先归位，在exchange中更换type类型，再更新动画，造成交换效果
            // 检测交换后的，是否有可以消除item
            if (this.exchangeIcon(data.anchor)) {
                // 执行消除消息
                this.handelMessage("cancel");
            } else {
                // 没有就执行换回来
                this.exchangeBack(data.anchor);
            }
        }
        // 执行消除
        else if (message == "cancel") {
            // 要消除的数目
            this.cancelNum = 0;
            for (var i = 1; i < this.row; i++) {
                for (var j = 1; j < this.col; j++) {
                    // 扫描到有一个要删除的，这个位置就执行消除动画
                    if (this.iconsDataTable[i][j].state == ICON_STATE_PRECANCEL2) {
                        //this.soundNode.getComponent("SoundControl").playExp();
                        AudioManager.instance.playAuidoEff("Explosion");
                        GameInfoManager.instance.score = GameInfoManager.instance.score + 1;// 加分
                        this.cancelNum = this.cancelNum + 1;// 计数
                        this.setIconState(i, j, ICON_STATE_CANCEL);// 执行具体动画
                    }
                }
            }
        }

        /*
        下降思路：
        1.在produce中把被消除的item的类型设为要下降的item的类型，依次往上推，直至没有下降的要随机生成
        2.在move中，其实类型已经确定，但是执行先把这个（被消除的）item向上定位，再向下执行动作，造成下降效果，依次推
        3.本不是实质的上面item下降，而是更换类型，定位+动作，造成动画效果
        */

        // 消除完了需要重新生成，制造下降的效果
        else if (message == "produce") {
            this.refreshScoreLabel();// 更新分数
            this.moveNum = 0;
            for (var i = 1; i < this.row; i++) {
                for (var j = 1; j < this.col; j++) {
                    if (this.iconsDataTable[i][j].state == ICON_STATE_CANCELED) {
                        this.moveNum = this.moveNum + 1;
                        // 设为移动状态在move消息中处理
                        this.setIconState(i, j, ICON_STATE_MOVE);

                        // 这里是下落的数量
                        this.iconsDataTable[i][j].moveNum = 0;
                        // 是否找到有下降的item
                        var isFind = false;
                        for (var k = (j + 1); k < this.col; k++) {
                            // 统计要下降多少个。在col - 1处的moveNum = 0，就是不要下降了
                            this.iconsDataTable[i][j].moveNum = this.iconsDataTable[i][j].moveNum + 1;
                            // 如果是竖排消除，找到第一个下降的item
                            if (this.iconsDataTable[i][k].state != ICON_STATE_CANCELED) {
                                // 把这个item设为canceled，为了下个j循环继续操作
                                this.iconsDataTable[i][k].state = ICON_STATE_CANCELED;
                                // 找到了item
                                isFind = true;
                                // 当前被消除的item的type设为要下降的item。
                                this.iconsDataTable[i][j].iconType = this.iconsDataTable[i][k].iconType;
                                break;
                            }
                        }
                        // 如果到顶了，10。就随机生成一个type
                        if (!isFind) {
                            this.iconsDataTable[i][j].iconType = this.getNewIconType(i, j);
                        }
                    }
                }
            }
            // 出来后，执行移动message
            this.handelMessage("move");
        }
        // 移动item,补充空白处
        else if (message == 'move') {
            // var finished = cc.callFunc(function(target){
            //     this.moveNum = this.moveNum -1;
            //     if(this.moveNum == 0){
            //         this.handelMessage("check");
            //     }
            // }, this);
            for (var i = 1; i < this.row; i++) {
                for (var j = 1; j < this.col; j++) {
                    // 扫描到移动的item
                    if (this.iconsDataTable[i][j].state == ICON_STATE_MOVE) {
                        // 播放声音
                        //this.soundNode.getComponent("SoundControl").playDrop();
                        AudioManager.instance.playAuidoEff("Drop");
                        // 设置类型
                        this.setIconNormalAnimObj(this.iconsDataTable[i][j]);
                        // 原始位置
                        var pos = this.iconsTable[i][j].getPosition();
                        // 消除多少个
                        var num = this.iconsDataTable[i][j].moveNum;
                        // 设置定位
                        this.iconsTable[i][j].setPosition(this.iconsTable[i][j + num].getPosition());
                        // 执行动作
                        //this.iconsTable[i][j].runAction(cc.sequence(cc.moveTo(0.1 * num, pos.x, pos.y), finished));
                        tween(this.iconsTable[i][j]).to(0.1 * num, { position: new Vec3(pos.x, pos.y, 0) }).call(() => {
                            this.moveNum = this.moveNum - 1;
                            if (this.moveNum == 0) {
                                this.handelMessage("check");
                            }
                        }).start();
                    }
                }
            }
        }
        // 再生成新item中检测是否有可以继续消除
        // 思路：水平和垂直扫描，是否存在预取消，确定取消状态
        else if (message == "check") {
            var isCancelV = false;
            var isCancelH = false;
            // 检测垂直方向
            for (var i = 1; i < this.row; i++) {
                var isCancel = this.checkCancelV(i);
                if (isCancel) {
                    isCancelV = true;
                }
            }
            if (isCancelV) {
                this.setCancelEnsure();
            }
            // 检测水平方向
            for (var j = 1; j < this.col; j++) {
                var isCancel = this.checkCancelH(j);
                if (isCancel) {
                    isCancelH = true;
                }
            }
            if (isCancelH) {
                this.setCancelEnsure();
            }
            // 执行取消消息
            if (isCancelH || isCancelV) {
                this.handelMessage("cancel");
            }
        }
        // 
    }
    // 为了设为确定取消状态
    // 先交换两个物体，然后检测水平和垂直方向是否可以消除
    exchangeIcon(anchor) {

        // console.log("交换前是否选中物体："+this.isControl);
        // 点击的item
        var oneIcon = this.iconsDataTable[this.chooseIconPos.x][this.chooseIconPos.y];
        // 要与之交换的item
        var anotherIcon;
        // 左边
        if (anchor == 1) {
            anotherIcon = this.iconsDataTable[this.chooseIconPos.x - 1][this.chooseIconPos.y];
        } else if (anchor == 2) {
            // 上
            anotherIcon = this.iconsDataTable[this.chooseIconPos.x][this.chooseIconPos.y + 1];
        } else if (anchor == 3) {
            // 右
            anotherIcon = this.iconsDataTable[this.chooseIconPos.x + 1][this.chooseIconPos.y];
        } else if (anchor == 4) {
            // 下
            anotherIcon = this.iconsDataTable[this.chooseIconPos.x][this.chooseIconPos.y - 1];
        }
        if (!anotherIcon) {
            console.log("没有选择到物体");
            return;
        }
        // 执行动画
        // 进行交换类型，并且更新动画clip，没有动态的效果
        // 注意这里：交换效果前，先归位，在exchange中更换type类型，再更新动画，造成交换效果
        var typeVal = oneIcon.iconType;// 点击的item的type
        oneIcon.iconType = anotherIcon.iconType;
        anotherIcon.iconType = typeVal;
        this.setIconNormalAnimObj(oneIcon);
        this.setIconNormalAnimObj(anotherIcon);
        // console.log("交换前是否选中物体："+this.isControl);

        var isCancel = [false, false, false];
        // 根据不同的交换方向判断水平和垂直是否可以消除
        // 得都确定执行消除状态，以免后面再进行检测会将已设为确定执行消除状态的变为待取消状态
        /*
        比如：向左滑
        1. 进行水平检测，将都设为确定消除
        2. 再进行两次垂直检测，会将左滑的设为普通状态
        3. 最后一个setEnsure，将不会把检测这个普通状态的item
        */
        if (anchor == 1) {// 左
            // 水平方向检测一次
            isCancel[1] = this.checkCancelH(this.chooseIconPos.y);
            // 确定执行消除的状态
            this.setCancelEnsure();
            // 垂直方向检测两次，一次是交换前的位置，一次是交换后的位置
            isCancel[2] = this.checkCancelV(this.chooseIconPos.x);
            this.setCancelEnsure();
            isCancel[3] = this.checkCancelV(this.chooseIconPos.x - 1);
        } else if (anchor == 2) {// 上
            // 水平方向检测两次，一次交换前的位置，一次是交换后的位置
            isCancel[1] = this.checkCancelH(this.chooseIconPos.y);
            this.setCancelEnsure();
            isCancel[2] = this.checkCancelH(this.chooseIconPos.y + 1);
            this.setCancelEnsure();
            // 垂直方向检测一次
            isCancel[3] = this.checkCancelV(this.chooseIconPos.x);
        } else if (anchor == 3) {// 右
            // 水平方向检测一次
            isCancel[1] = this.checkCancelH(this.chooseIconPos.y);
            this.setCancelEnsure();
            // 垂直方向检测两次，一次交换前的位置，一次是交换后的位置
            isCancel[2] = this.checkCancelV(this.chooseIconPos.x);
            this.setCancelEnsure();
            isCancel[3] = this.checkCancelV(this.chooseIconPos.x + 1);
        } else if (anchor == 4) {// 下
            // 水平方向检测两次，一次交换前的位置，一次是交换后的位置
            isCancel[1] = this.checkCancelH(this.chooseIconPos.y);
            this.setCancelEnsure();
            isCancel[2] = this.checkCancelH(this.chooseIconPos.y - 1);
            this.setCancelEnsure();
            // 垂直方向检测一次
            isCancel[3] = this.checkCancelV(this.chooseIconPos.x);
        }
        // 对已有准备取消的都设置为确定取消状态
        this.setCancelEnsure();
        // 有一个交换就交换，而不换回去
        return (isCancel[1] || isCancel[2] || isCancel[3])
    }
    // 执行换回两个交换后没有消除item的item
    exchangeBack(anchor) {
        /*
        思路：
        1. 获取两个item的位置
        2. 执行互相到另外item的位置动作（表面动画）
        3. 执行完返回的函数，重新定位两个item具体位置，再更换类型，执行默认动画，（实质更换）
        */
        var oneIconData = this.iconsDataTable[this.chooseIconPos.x][this.chooseIconPos.y];
        // 获取实体pre
        var oneIconItem = this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y];
        var anotherIconData, anotherIconItem;
        if (anchor == 1) {// 左
            anotherIconData = this.iconsDataTable[this.chooseIconPos.x - 1][this.chooseIconPos.y];
            anotherIconItem = this.iconsTable[this.chooseIconPos.x - 1][this.chooseIconPos.y];
        } else if (anchor == 2) {// 上
            anotherIconData = this.iconsDataTable[this.chooseIconPos.x][this.chooseIconPos.y + 1];
            anotherIconItem = this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y + 1];
        } else if (anchor == 3) {// 右
            anotherIconData = this.iconsDataTable[this.chooseIconPos.x + 1][this.chooseIconPos.y];
            anotherIconItem = this.iconsTable[this.chooseIconPos.x + 1][this.chooseIconPos.y];
        } else if (anchor == 4) {// 下
            anotherIconData = this.iconsDataTable[this.chooseIconPos.x][this.chooseIconPos.y - 1];
            anotherIconItem = this.iconsTable[this.chooseIconPos.x][this.chooseIconPos.y - 1];
        }
        if (!anotherIconData || !anotherIconItem || !oneIconData) {
            return;
        }
        // 获取默认位置
        var pos1 = oneIconItem.getPosition();
        var pos2 = anotherIconItem.getPosition();
        // 返回函数
        // var finished = cc.callFunc(function(target){
        //     var typeVal = oneIconData.iconType;
        //     oneIconData.iconType = anotherIconData.iconType;
        //     anotherIconData.iconType = typeVal;
        //     // 执行动画
        //     this.setIconNormalAnimObj(oneIconData);
        //     this.setIconNormalAnimObj(anotherIconData);
        //     oneIconItem.setPosition(pos1);
        //     anotherIconItem.setPosition(pos2);// 归位
        // }, this);// 注意不要少了this
        // 执行动作
        //oneIconItem.runAction(cc.sequence(cc.moveTo(0.5, pos2.x, pos2.y), finished));
        //anotherIconItem.runAction(cc.moveTo(0.5, pos1.x, pos1.y));

        tween(anotherIconItem).to(0.5, { position: new Vec3(pos1.x, pos1.y, 0) }).start();
        tween(oneIconItem).to(0.5, { position: new Vec3(pos2.x, pos2.y, 0) }).call(() => {
            var typeVal = oneIconData.iconType;
            oneIconData.iconType = anotherIconData.iconType;
            anotherIconData.iconType = typeVal;
            // 执行动画
            this.setIconNormalAnimObj(oneIconData);
            this.setIconNormalAnimObj(anotherIconData);
            oneIconItem.setPosition(pos1);
            anotherIconItem.setPosition(pos2);// 归位
        }).start();
    }
    // 检测水平方向是否可以消除
    checkCancelH(col) {
        // 思路：从左边往右边扫描，有相同的连续块，标记为可以消除，记得恢复不能消除的块
        // 计数多少个相同块
        var cancelNum = 1;
        // 从第一个开始 
        // 要匹配的item类型
        var iconType = this.iconsDataTable[1][col].iconType;
        var isCancel = false;// 判断这一行是否有取消的块
        var cancelStartPos = 1; // 记录消除开始位置
        // 默认标为可以取消
        this.setIconState(1, col, ICON_STATE_PRECANCEL);
        // 从左向右扫描
        for (var i = 2; i < this.row; i++) {
            // 连续相同块
            if (iconType == this.iconsDataTable[i][col].iconType) {
                cancelNum = cancelNum + 1;
                this.setIconState(i, col, ICON_STATE_PRECANCEL);
                // 积分
                if (cancelNum >= 3) {
                    isCancel = true;
                    GameInfoManager.instance.score = GameInfoManager.instance.score + cancelNum;
                }
            } else {
                // 当刚好前面3个可以消除时候，不能恢复他们的状态
                // 得只有前面1个或者2个标为消除状态，可以恢复
                if (cancelNum < 3) {
                    // 不是相同连续块，恢复之前一个或者两个可以消除的状态
                    for (var k = (i - 1); k > 0; k--) {
                        if (iconType == this.iconsDataTable[k][col].iconType) {
                            // 恢复
                            this.setIconState(k, col, ICON_STATE_NORMAL);
                        } else {
                            // 这里跳出是防止当前面设为消除状态，被设回普通状态
                            break;
                        }
                    }
                }
                /*
                只有大于还剩3个item时，这个item可以继续默认标为取消状态
                如果小于3个item时，就不用标为取消状态了，默认为普通状态
                可以消除的话会在上面标为取消状态
                */
                if (i < (this.row - 2)) {
                    cancelNum = 1;
                    cancelStartPos = i; // 更新消除开始位置
                    // 更新要匹配的item类型
                    iconType = this.iconsDataTable[i][col].iconType;
                    this.setIconState(i, col, ICON_STATE_PRECANCEL);
                }
                else {
                    break;// 如果倒数第二个都不等于倒数第三个，那么自然就可以退出了
                }
            }
        }
        return isCancel;
    }
    // 检测垂直方向是否可以消除
    checkCancelV(row) {
        // 思路：从下边往上边扫描，有相同的连续块，标记为可以消除，记得恢复不能消除的块
        // 计数多少个相同块
        var cancelNum = 1;
        // 从第一个开始
        var iconType = this.iconsDataTable[row][1].iconType;
        var isCancel = false;// 判断这一列是否有取消的块
        var cancelStartPos = 1; // 记录消除开始位置
        // 默认标为可以取消
        this.setIconState(row, 1, ICON_STATE_PRECANCEL);
        // 从下向上扫描
        for (var i = 2; i < this.col; i++) {
            // 连续相同块
            if (iconType == this.iconsDataTable[row][i].iconType) {
                cancelNum = cancelNum + 1;
                this.setIconState(row, i, ICON_STATE_PRECANCEL);
                // 积分
                if (cancelNum >= 3) {
                    isCancel = true;
                    GameInfoManager.instance.score = GameInfoManager.instance.score + cancelNum;
                }
            } else {
                // 当刚好前面3个可以消除时候，不能恢复他们的状态
                // 得只有前面1个或者2个标为消除状态，可以恢复
                if (cancelNum < 3) {
                    // 不是相同连续块，恢复之前一个或者两个可以消除的状态
                    for (var k = (i - 1); k > 0; k--) {
                        if (iconType == this.iconsDataTable[row][k].iconType) {
                            // 恢复
                            this.setIconState(row, k, ICON_STATE_NORMAL);
                        } else {
                            // 这里跳出是防止当前面设为消除状态，被设回普通状态
                            break;
                        }
                    }
                }
                /*
                只有大于还剩3个item时，可以继续默认标为取消状态
                如果小于3个item时，就不用标为取消状态了，默认为普通状态
                可以消除的话会在上面标为取消状态
                */
                if (i < (this.col - 2)) {
                    cancelNum = 1;
                    cancelStartPos = i; // 更新消除开始位置
                    iconType = this.iconsDataTable[row][i].iconType;
                    this.setIconState(row, i, ICON_STATE_PRECANCEL);
                } else {
                    break;// 如果倒数第二个都不等于倒数第三个，那么自然就可以退出了
                }
            }
        }
        return isCancel;
    }
    // 执行设置确定消除的状态
    setCancelEnsure() {
        for (var i = 1; i < this.row; i++) {
            for (var j = 1; j < this.col; j++) {
                if (this.iconsDataTable[i][j].state == ICON_STATE_PRECANCEL) {
                    this.setIconState(i, j, ICON_STATE_PRECANCEL2);
                }
            }
        }
    }
    // 辅助方法--------
    // 生成小物块的类型数据
    getNewIconType(i, j) {
        var exTypeTable = [-1, -1];
        if (i > 1) {
            exTypeTable[1] = this.iconsDataTable[i - 1][j].iconType;// 获取上一行的类型
        }
        if (j > 1) {
            exTypeTable[2] = this.iconsDataTable[i][j - 1].iconType;// 获取上一列的类型
        }
        var typeTable = [];
        var max = 0;
        for (let i = 1; i < this.typeNum; i++) {
            // 如果旁边没有生成这个类型，自己就为这个类型
            if (i != exTypeTable[1] && i != exTypeTable[2]) {
                max = max + 1;
                typeTable[max] = i;
            }
        }
        return typeTable[this.getRandomInt(1, max)];//随机
    }
    // 刷新分数
    refreshScoreLabel() {
        this.scoreLabel.string = "" + GameInfoManager.instance.score;
        // 时间增加
        GameInfoManager.instance.calTimer = 0;
        let addtime = GameInfoManager.instance.currentTimer + 1;
        GameInfoManager.instance.currentTimer = addtime <= GameInfoManager.instance.totalTimer ? addtime : GameInfoManager.instance.totalTimer;
    }


    // Global.js
    get_board_row() {
        return BOARD_ROW;
    }

    get_board_col() {
        return BOARD_COL;
    }

    getRandomInt(min, max) {
        return Math.floor(Math.random() * (max - min)) + min;//min - max随机
    }
}

