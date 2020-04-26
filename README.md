# 2D-Platform-Controller-_-Bardent

## 说明
1. 这是学习一个来自youtube的一个教程
2. 希望你可以坚持学完
3. https://www.youtube.com/watch?v=Pux1GlFwKPs



## BUG

1. 按着前进再跳跃依附到墙壁后没法触发 “可跳跃”

   - 优先级1

   - 跳跃前进没问题

   - 前进跳跃才有问题

   - 使用 IsWallSliding => jumpsLeft = 1 不得行

     

2. 二段跳中大跳无法再“小跳”

   - 优先级3
   - 小跳后可以大跳
   - 但是大跳后无法再小跳
   - 仅在多段跳中有问题
   - 如果游戏设定为单段跳跃则无须fix

