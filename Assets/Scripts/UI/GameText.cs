namespace KarakuriLabo
{
    // Player-facing copy uses kana and short, concrete instructions. Asset and
    // scene identifiers remain stable so wording never changes gameplay wiring.
    public static class GameText
    {
        public const string Play = "スタート";
        public const string Reset = "やりなおす";
        public const string Adjust = "なおす";
        public const string Retry = "もういちど";
        public const string Clear = "やったね！";
        public const string TryAgain = "おしい！";
        public const string EditMode = "どうぐを おこう";
        public const string Playing = "うごいているよ";
        public const string SoundOn = "おと：あり";
        public const string SoundOff = "おと：なし";
        public const string EditHint = "どうぐを おいて ボールを ゴールへ";
        public const string PlayingHint = "「やりなおす」で どうぐを なおせるよ";
        public const string FailedHint = "「なおす」で ばしょを かえよう ／ 「もういちど」で スタート";
        public const string ClearHint = "やったね！ 「つぎへ」で すすもう";
        public const string AllClearHint = "ぜんぶ クリア！ よく できたね！";
        public const string CameraHint = "ゆび1ぽんで くるくる\nゆび2ほんで いどう\nゆびを ひろげて おおきく";

        public static string PartName(string name)
        {
            switch (name)
            {
                case "RAMP": return "さか";
                case "SEESAW": return "シーソー";
                case "DOMINO": return "ドミノ";
                case "HIGH": return "たかい さか";
                case "LOW": return "ひくい さか";
                case "UPPER": return "うえの さか";
                case "MIDDLE": return "なかの さか";
                case "LOWER": return "したの さか";
                case "SPRING": return "ばね";
                case "CURVE": return "カーブ";
                case "FUNNEL": return "じょうご";
                case "LIFT": return "リフト";
                case "FAN": return "せんぷうき";
                case "PART": return "どうぐ";
                default: return string.IsNullOrWhiteSpace(name) ? "どうぐ" : name;
            }
        }

        public static string Failure(AttemptFailure reason)
        {
            switch (reason)
            {
                case AttemptFailure.Fell:
                    return "ボールが コースから おちたよ\nさかや どうぐを つないでみよう";
                case AttemptFailure.GoalRequirement:
                    return "まだ どうぐを つかっていないよ\nボールが とおる ばしょに おこう";
                case AttemptFailure.TimedOut:
                    return "ゴールに とどかなかったね\nどうぐの むきを かえてみよう";
                default:
                    return "ボールが とまったよ\nどうぐの ばしょを かえてみよう";
            }
        }

        public static string StageTitle(string scene)
        {
            switch (scene)
            {
                case "StageOne": return "はじめの さか";
                case "StageThree": return "ゆらゆら シーソー";
                case "StageFour": return "おくから てまえへ";
                case "StageFive": return "ながい くだりみち";
                case "StageSpring": return "ぴょんと ジャンプ";
                case "StageCurve": return "くるっと カーブ";
                case "StageFunnel": return "すとんと おとそう";
                case "StageLift": return "ぐんぐん のぼろう";
                case "StageFan": return "かぜで ころがそう";
                case "StageTwo": return "ドミノを たおそう";
                default: return "じゆうに あそぼう";
            }
        }

        public static string StageHint(string scene, bool selected)
        {
            switch (scene)
            {
                case "StageOne": return selected ? "さかを ボールの したへ うごかそう" : "「さか」を おいて ボールを ゴールへ";
                case "StageThree": return selected ? "ボールが のる ばしょに シーソーを おこう" : "シーソーを ゆらして ゴールしよう";
                case "StageFour": return selected ? "さかの むきを まわして つなごう" : "2つの さかで おくから てまえへ";
                case "StageFive": return selected ? "たかさと むきを あわせて つなごう" : "うえ・なか・したの さかを つなごう";
                case "StageSpring": return selected ? "ばねの むきを ゴールへ むけよう" : "ばねで ボールを ジャンプさせよう";
                case "StageCurve": return selected ? "カーブを さかの さきに つなごう" : "カーブで ボールを まげてみよう";
                case "StageFunnel": return selected ? "ひろい くちを ボールの したに おこう" : "じょうごで ボールを うけとめよう";
                case "StageLift": return selected ? "ボールが のる ばしょに リフトを おこう" : "リフトで ボールを うえへ はこぼう";
                case "StageFan": return selected ? "せんぷうきを ボールに むけよう" : "せんぷうきの かぜで さかを のぼろう";
                case "StageTwo": return selected ? "ドミノを スイッチまで ならべよう" : "ドミノを たおして とびらを あけよう";
                default: return selected ? "どうぐを おしたまま うごかそう" : EditHint;
            }
        }
    }
}
