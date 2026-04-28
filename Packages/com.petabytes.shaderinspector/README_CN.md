# ShaderInspector

通过 [malioc](https://developer.arm.com/Tools%20and%20Software/Mali%20Offline%20Compiler) 离线shader编译工具分析shader的性能指标。

# 使用方法

首次使用时需要安装 Mali Offline Shader Compiler，点击访问 arm 官网来安装，或者如果你已经安装，点击 `...` 选择 malioc 可执行文件所在的路径。
![mali path](Docs~/mali_path.png)

它的路径一般在 `Arm Performance Studio XXXX/mali_offline_compiler`

![mali path](Docs~/mali_path_selection.png)


主界面
![UI](Docs~/UI.png)

*  打开package manager，从本地添加package
*  点击 Window => Analysis => Shader Inspector 打开工具
*  根据目标机型选择GPU（不同型号性能指标差别很大）
*  选择shader，subshader index，pass index
*  选择开启的keyword，或者直接从材质导入开启的keywords
*  点击analyze

分析报告

![UI](Docs~/report.png)

* 报告分为vertex和fragment，每项指标悬停可以查看含义。
* 文字颜色反映当前shader的指标与上次的报告的变化，红色表示增大，绿色减小，黑色不变。
* 选择 Compare with Reference 可以查看urp lit作为参照标准的指标，颜色表示与urp lit相比的差异。
  
![UI](Docs~/compare.png)

变体的 GLSL 代码

![UI](Docs~/code.png)

# 性能报告的含义

![UI](Docs~/help.png)

点击问号可以跳转至Arm的帮助文档。

报告分为 vertex 和 fragment 两部分。其中 vertex 在 mali gpu 上分为 position 和 varying 两块，varying 的指令只有可见的部分才会执行，position 则是每个顶点索引都执行。

主要关注的是：
*  register 的使用数量，register越少，gpu的能并行处理的线程越多。
*  半精度运算的占比(16-bit Arithmetic)，半精度运算消耗是单精度的一半。
*  shader 指令周期(cycles)的数量，指令越多，执行消耗越高，指令分为:
   *  数学运算(arith_total)，在valhall上又划分为：
      *  Fused multiply accumulate(arith_fma)。
      *  Arithmetic conversion(arith_cvt)。
      *  Special functions unity(arith_sfu)。
   *  存储访问(load_store)。
   *  纹理采样(texture)。
*  [shader properties](https://developer.arm.com/documentation/101863/0800/Using-Mali-Offline-Compiler/Performance-analysis/Shader-properties?lang=en)，如：
   *  Has uniform computation，uniform变量的运算，渲染时结果不发生改变，可以移到shader之外进行
   *  Modifies coverage，是否改变了可见性，影响early z是否生效

## 参考资料：
* [Arm Mali Offline Compiler User Guide](https://developer.arm.com/documentation/101863/0800?lang=en)
* [Arm Mali GPU Training - Episode 3.5: Mali Offline Compiler](https://developer.arm.com/Additional%20Resources/Video%20Tutorials/Arm%20Mali%20GPU%20Training%20-%20EP3-5)
* [IDVS shader variants](https://developer.arm.com/documentation/101863/0800/Using-Mali-Offline-Compiler/Performance-analysis/IDVS-shader-variants)
