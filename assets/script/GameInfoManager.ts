import { _decorator, Component, Node } from 'cc';
const { ccclass, property } = _decorator;

@ccclass('GameInfoManager')
export class GameInfoManager {
    private static _instance: GameInfoManager;
    static get instance() {
        if (this._instance) {
            return this._instance;
        }

        this._instance = new GameInfoManager();
        return this._instance;
    }
    public isOver: boolean = false;
    public isStop: boolean = false;

    // 游戏时间
    private gameTime: number = 120;
    public totalTimer: number = this.gameTime;
    public currentTimer: number = this.gameTime;
    public calTimer: number = 0;

    public score: number = 0;

    // 随机次数
    public resetNum: number = 2;

    start() {

    }
    reStart() {
        this.resetNum = 2;
        this.score = 0;
        this.isOver = false;
        this.isStop = false;
        this.totalTimer = this.gameTime;
        this.currentTimer = this.gameTime;
        this.calTimer = 0;
    }

    update(deltaTime: number) {

    }
}

