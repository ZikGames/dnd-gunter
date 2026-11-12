using Avalonia.Controls;
using DNDHelper.ViewModels.Config;
using DNDHelper.ViewModels.Inventory;
using DNDHelper.ViewModels.Сharacteristics;
using DNDHelper.Views;

namespace DNDHelper.ViewModels.Character
{
    class AttributesCharacter
    {
        public static void CallAllMethodInScript()
        {
            UpdateRolls();
            СountAvailableActions();
            StickMethod();
        }


        public static void UpdateRolls()
        {
            MainWindow main = MainWindow.Instance;
            main.character_rollattack_textblock.Text = (ItemBaffsListScript.ItemBaffs[30][0] + CharacteristicTable.OtherBaff(30) + Effects.EffectBaffs[31][0]).ToString();
            main.character_block_textblock.Text = (ItemBaffsListScript.ItemBaffs[31][0] + CharacteristicTable.OtherBaff(31) + Effects.EffectBaffs[31][0] + Effects.EffectBaffs[28][0]).ToString();
            main.character_dodge_textblock.Text = (ItemBaffsListScript.ItemBaffs[32][0] + CharacteristicTable.OtherBaff(32) + Effects.EffectBaffs[31][0]).ToString();
            main.character_counteraction_textblock.Text = (ItemBaffsListScript.ItemBaffs[33][0] + CharacteristicTable.OtherBaff(33) + Effects.EffectBaffs[31][0]).ToString();
            main.character_fistattack_textblock.Text = (ItemBaffsListScript.ItemBaffs[34][0] + Effects.EffectBaffs[31][0]).ToString();
            main.character_longrangeattack_textblock.Text = (ItemBaffsListScript.ItemBaffs[35][0] + Effects.EffectBaffs[31][0]).ToString();

            // WPF: new ToolTip { Content = "..." } назначался в свойство .ToolTip.
            // В Avalonia ToolTip — присоединённое свойство, ставится через ToolTip.SetTip(control, content).
            ToolTip.SetTip(main.character_rollattackname_textblock,
                $"Предметы:{ItemBaffsListScript.ItemBaffs[30][0]} Эффекты{Effects.EffectBaffs[31][0]} Остальное:{CharacteristicTable.OtherBaff(30)}");
            ToolTip.SetTip(main.character_blockname_textblock,
                $"Предметы:{ItemBaffsListScript.ItemBaffs[31][0]} Эффекты:{Effects.EffectBaffs[31][0] + Effects.EffectBaffs[33][0]} Остальное:{CharacteristicTable.OtherBaff(31)}");
            ToolTip.SetTip(main.character_dodgename_textblock,
                $"Предметы:{ItemBaffsListScript.ItemBaffs[32][0]} Эффекты:{Effects.EffectBaffs[31][0]} Остальное:{CharacteristicTable.OtherBaff(32)}");
            ToolTip.SetTip(main.character_counteractionname_textblock,
                $"Предметы:{ItemBaffsListScript.ItemBaffs[33][0]} Эффекты:{Effects.EffectBaffs[31][0]} Остальное:{CharacteristicTable.OtherBaff(33)}");
            ToolTip.SetTip(main.character_fistattackname_textblock,
                $"Предметы:{ItemBaffsListScript.ItemBaffs[34][0]} Эффекты:{Effects.EffectBaffs[31][0]}");
            ToolTip.SetTip(main.character_longrangeattackname_textblock,
                $"Предметы:{ItemBaffsListScript.ItemBaffs[35][0]} Эффекты:{Effects.EffectBaffs[31][0]}");
        }

        public static void СountAvailableActions()
        {
            int actions = 1;
            int counterActions = 1;

            if (CharacteristicTable.Buffed(CharacteristicTable.StatName.Agility) >= 20)
            {
                if (CharacteristicTable.Buffed(CharacteristicTable.StatName.Agility) >= 30)
                {
                    actions = 3;
                    counterActions = 3;
                }
                actions = 2;
                counterActions = 2;
            }

            actions += ItemBaffsListScript.ItemBaffs[40][0];
            counterActions += ItemBaffsListScript.ItemBaffs[41][0];

            MainWindow.Instance.Actions_textblock.Text = actions.ToString();
            MainWindow.Instance.Counter_actions_textblock.Text = counterActions.ToString();
        }

        public static void StickMethod()
        {
            double lengthStick = Race.SelectedClassData.StandartStickLength;
            double multiplyLength = Race.SelectedClassData.MultiplyStickLength;

            int Agility = CharacteristicTable.Buffed(CharacteristicTable.StatName.Agility);
            double addStick = 0;
            if ((Agility - 10) >= 5)
                addStick = (int)((Agility - 10) / 5) * 0.5;
            lengthStick *= multiplyLength;

            lengthStick += addStick + ItemBaffsListScript.ItemBaffs[39][0];
            double multiplyEffect = Effects.EffectBaffs[34][0] * 0.01;
            if (multiplyEffect != 0)
                lengthStick *= multiplyEffect;
            MainWindow.Instance.Movesticks_textblock.Text = lengthStick.ToString();
        }
    }
}
