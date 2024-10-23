[![Test](https://github.com/swittlich/OneWare.CC-Toolchain/actions/workflows/test.yml/badge.svg)](https://github.com/swittlich/OneWare.CC-Toolchain/actions/workflows/test.yml)
[![Publish](https://github.com/swittlich/OneWare.CC-Toolchain/actions/workflows/publish.yml/badge.svg)](https://github.com/swittlich/OneWare.CC-Toolchain/actions/workflows/publish.yml)

# OneWare.CologneChip

An integration of the CologneChip toolchain for OneWare. 
The toolchain for the GateMate FPGA boards. For already provided boards have a look at the [GateMate repository](https://github.com/swittlich/OneWare.GateMate).

- [CologneChip](https://colognechip.com/)
- [Toolchain](https://www.colognechip.com/docs/ug1002-toolchain-install-latest.pdf)
- [OneWare](https://one-ware.com)

![image](https://raw.githubusercontent.com/swittlich/OneWare.CC-Toolchain/refs/heads/main/Icon.png)

# Installation Guide 

1. Install this Plugin
2. Download toolchain from [CologneChip](https://colognechip.com/mygatemate/) (it's free, but you will need an account).
3. Unzip the toolchain 
4. Set Path to toolchain in OneWareStudio Extras -> Settings ->  Tools -> CologneChip
5. Set the Toolchain in OneWareStudio to "CologneChip"
6. (optional) Add '*.ccf' files to your project fpgaproj-File

# Special thanks

Thanks to Dr. Michael Gude from CologneChips AG for allowing us to integrate his pictures and logos for this tool chain.

# Known errors and workarounds

# Yosys error code != 0
The synthesis can run, but it can happen that the error code of the synthesis is not equal to 0. The normal behaviour of the program is then not to continue working with the P_R tool. In this case, however, it may be desirable to ignore the error code. 

Can be set via Extras -> Setting -> Tools -> CologneChip -> ‘ignore an exit code not equal to 0 after synthesis’

# Ignore UI for HardwarePin Mapping

Currently, OneWare studio cannot always find all signals in and out of the Top Level Entity (see also this [issue on GitHub](https://github.com/one-ware/OneWare/issues/18)). 
If this is the case, then the pin mapping cannot be done via the UI, which means that no CCF file can be generated or an existing mapping in an existing ccf file is overwritten with nothing or commented out. To prevent this, the ignoreGUI option has been introduced. 
This can be set as follows:  
Extras -> Setting -> Tools -> CologneChip -> ‘Ignore UI for HardwarePin Mapping’