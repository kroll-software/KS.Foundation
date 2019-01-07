using System;

namespace KS.Foundation
{
	public class Dos2Win
	{
		private char[] WinMap;
		private char[] DosMap;
		
		public Dos2Win() {
			WinMap = new char[257];
			DosMap = new char[257];
			
			InitWinMap();
			InitDosMap();
		}
		
		private void InitWinMap ()
		{
			int i;
			
			for (i = 0; i <= 175; i++)
			{
                WinMap[i] = Strings.Chr(i);
			}

            WinMap[21] = Strings.Chr(167);
			WinMap[128] = Strings.Chr(199);
			WinMap[129] = Strings.Chr(252);
			WinMap[130] = Strings.Chr(233);
			WinMap[131] = Strings.Chr(226);
			WinMap[132] = Strings.Chr(228);
			WinMap[134] = Strings.Chr(229);
			WinMap[135] = Strings.Chr(231);
			WinMap[136] = Strings.Chr(234);
			WinMap[137] = Strings.Chr(235);
			WinMap[138] = Strings.Chr(232);
			WinMap[139] = Strings.Chr(239);
			WinMap[140] = Strings.Chr(238);
			WinMap[141] = Strings.Chr(236);
			WinMap[142] = Strings.Chr(196);
			WinMap[143] = Strings.Chr(197);
			WinMap[144] = Strings.Chr(193);
			WinMap[145] = Strings.Chr(230);
			WinMap[146] = '\u0020';
			WinMap[147] = Strings.Chr(243);
			WinMap[148] = Strings.Chr(246);
			WinMap[149] = Strings.Chr(242);
			WinMap[150] = Strings.Chr(250);
			WinMap[151] = Strings.Chr(249);
			WinMap[152] = Strings.Chr(255);
			WinMap[153] = Strings.Chr(214);
			WinMap[154] = Strings.Chr(220);
			WinMap[155] = Strings.Chr(162);
			WinMap[156] = Strings.Chr(163);
			WinMap[157] = Strings.Chr(165);
			WinMap[158] = '\u0020';
			WinMap[159] = '\u0020';
			WinMap[160] = Strings.Chr(227);
			WinMap[161] = Strings.Chr(237);
			WinMap[162] = Strings.Chr(243);
			WinMap[163] = Strings.Chr(250);
			WinMap[164] = Strings.Chr(241);
			WinMap[165] = Strings.Chr(209);
			WinMap[166] = Strings.Chr(170);
			WinMap[167] = Strings.Chr(186);
			WinMap[168] = Strings.Chr(191);
			WinMap[169] = '\u0020';
			WinMap[170] = Strings.Chr(172);
			WinMap[171] = Strings.Chr(189);
			WinMap[172] = Strings.Chr(188);
			WinMap[173] = '\u0020';
			WinMap[174] = Strings.Chr(171);
			WinMap[175] = Strings.Chr(187);
			
			for (i = 176; i <= 255; i++)
			{
				WinMap[i] = '\u0020';
			}
			
			WinMap[225] = Strings.Chr(223);
			WinMap[229] = Strings.Chr(229);
			WinMap[237] = Strings.Chr(248);
			WinMap[241] = Strings.Chr(177);
			WinMap[246] = Strings.Chr(246);
			WinMap[248] = Strings.Chr(176);
			WinMap[253] = Strings.Chr(178);
			WinMap[255] = Strings.Chr(152);
		}
		
		private void InitDosMap ()
		{
			int i;
			
			for (i = 0; i <= 126; i++)
			{
				DosMap[i] = Strings.Chr(i);
			}
			
			for (i = 127; i <= 161; i++)
			{
				DosMap[i] = '\u0020';
			}
			
			DosMap[162] = Strings.Chr(155);
			DosMap[163] = Strings.Chr(156);
			DosMap[164] = '\u0024';
			DosMap[165] = Strings.Chr(157);
			DosMap[166] = '\u007C';
			DosMap[167] = '\u0015';
			DosMap[168] = '\u0020';
			DosMap[169] = '\u0020';
			DosMap[170] = Strings.Chr(166);
			DosMap[171] = Strings.Chr(174);
			DosMap[172] = Strings.Chr(170);
			DosMap[175] = Strings.Chr(196);
			DosMap[176] = Strings.Chr(248);
			DosMap[177] = Strings.Chr(241);
			DosMap[178] = Strings.Chr(253);
			DosMap[179] = '\u0020';
			DosMap[180] = '\u0020';
			DosMap[181] = Strings.Chr(230);
			DosMap[182] = '\u0020';
			DosMap[183] = '\u0020';
			DosMap[184] = '\u0020';
			DosMap[185] = '\u0020';
			DosMap[186] = Strings.Chr(167);
			DosMap[187] = Strings.Chr(175);
			DosMap[188] = Strings.Chr(172);
			DosMap[189] = Strings.Chr(172);
			DosMap[190] = '\u0020';
			DosMap[191] = Strings.Chr(168);
			DosMap[192] = Strings.Chr(143);
			DosMap[193] = Strings.Chr(143);
			DosMap[194] = '\u0020';
			DosMap[195] = '\u0020';
			DosMap[196] = '\u0020';
			DosMap[197] = '\u0020';
			DosMap[198] = Strings.Chr(146);
			DosMap[199] = Strings.Chr(128);
			DosMap[200] = Strings.Chr(144);
			DosMap[201] = Strings.Chr(155);
			DosMap[202] = '\u0020';
			DosMap[203] = Strings.Chr(137);
			DosMap[204] = Strings.Chr(141);
			DosMap[205] = Strings.Chr(161);
			DosMap[206] = Strings.Chr(140);
			DosMap[207] = Strings.Chr(139);
			DosMap[208] = '\u0020';
			DosMap[209] = Strings.Chr(165);
			DosMap[210] = Strings.Chr(149);
			DosMap[211] = Strings.Chr(162);
			DosMap[212] = Strings.Chr(147);
			DosMap[213] = '\u0020';
			DosMap[214] = Strings.Chr(153);
			DosMap[215] = '\u0020';
			DosMap[216] = '\u0020';
			DosMap[217] = Strings.Chr(151);
			DosMap[218] = Strings.Chr(163);
			DosMap[219] = Strings.Chr(150);
			DosMap[220] = Strings.Chr(154);
			DosMap[221] = '\u0020';
			DosMap[222] = '\u0020';
			DosMap[223] = Strings.Chr(225);
			DosMap[224] = Strings.Chr(134);
			DosMap[225] = Strings.Chr(160);
			DosMap[226] = Strings.Chr(131);
			DosMap[227] = '\u0020';
			DosMap[228] = Strings.Chr(132);
			DosMap[229] = Strings.Chr(134);
			DosMap[230] = Strings.Chr(145);
			DosMap[231] = Strings.Chr(128);
			DosMap[232] = Strings.Chr(138);
			DosMap[233] = Strings.Chr(130);
			DosMap[234] = Strings.Chr(136);
			DosMap[235] = Strings.Chr(137);
			DosMap[236] = Strings.Chr(141);
			DosMap[237] = Strings.Chr(161);
			DosMap[238] = Strings.Chr(140);
			DosMap[239] = Strings.Chr(139);
			DosMap[240] = '\u0020';
			DosMap[241] = Strings.Chr(164);
			DosMap[242] = Strings.Chr(149);
			DosMap[243] = Strings.Chr(162);
			DosMap[244] = Strings.Chr(147);
			DosMap[245] = '\u0020';
			DosMap[246] = Strings.Chr(148);
			DosMap[247] = Strings.Chr(247);
			DosMap[248] = Strings.Chr(237);
			DosMap[249] = Strings.Chr(151);
			DosMap[250] = Strings.Chr(163);
			DosMap[251] = Strings.Chr(150);
			DosMap[252] = Strings.Chr(129);
			DosMap[253] = '\u0020';
			DosMap[254] = '\u0020';
			DosMap[255] = Strings.Chr(152);
		}
		
		
		
		// --- >>> ASCI 2 Ansi (Nach WIN)
		
		public char Ascii2Ansi(char c)
		{
			try
			{
				return WinMap[(int)c];
			}
			catch (Exception)
			{
				return '\u0020';
			}
		}
		
		public string Ascii2Ansi(string S)
		{
			System.Text.StringBuilder SB = new System.Text.StringBuilder(S);			
			
			for (int i = 0; i < SB.Length; i++)
			{
				try
				{
					SB[i] = WinMap[(int)SB[i]];
				}
				catch (Exception)
				{
					SB[i] = '\u0020';
				}
			}
			
			return SB.ToString();
		}
		
		
		// --- >>> Ansi 2 ASCI (Nach DOS)
		
		public char Ansi2Ascii(char c)
		{
			try
			{
				return DosMap[(int)c];
			}
			catch (Exception)
			{
				return '\u0020';
			}
		}
		
		public string Ansi2Ascii(string S)
		{
			System.Text.StringBuilder SB = new System.Text.StringBuilder(S);			
			
			for (int i = 0; i < SB.Length; i++)
			{
				try
				{
					SB[i] = DosMap[(int)SB[i]];
				}
				catch (Exception)
				{
					SB[i] = '\u0020';
				}
			}
			
			return SB.ToString();
		}


        public bool IsAscii(string S)
        {
            //System.Text.StringBuilder SB = new System.Text.StringBuilder(S);

            int CountAscii = 0;
            int CountAnsi = 0;

            int iasc = 0;

            //for (int i = 0; i < SB.Length; i++)
            foreach (char c in S)
            {
                //iasc = Strings.Asc(SB[i]);
                iasc = (int)c;
                if (iasc > 32 && iasc < 256)
                {
                    switch (iasc)
                    {
                        case 132: // ä ö ü
                            CountAscii++;
                            break;

                        case 148:
                            CountAscii++;
                            break;

                        case 129:
                            CountAscii++;
                            break;

                        case 142: // Ä Ö Ü
                            CountAscii++;
                            break;

                        case 153:
                            CountAscii++;
                            break;

                        case 154:
                            CountAscii++;
                            break;

                        case 225: // ß						
                            CountAscii++;
                            break;

                        default:
                            if (DosMap[iasc] == '\u0020')
                                CountAnsi++;

                            break;
                    }
                }
            }

            return CountAscii > CountAnsi;
        }        
	}
	
}
