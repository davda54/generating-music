   M S B A M L   `   `   `  ����  	PianoRoll    PianoRoll.MainWindow   5   	   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   -   5          � Title$  
Piano Roll��5   	   $	��720��6   $
��1024��6)   ���	#FF323232 � WindowStartupLocation$ CenterScreen=�6�    � WindowState$ 	Maximized=�6�   .��5      � -   5         �Name$ VerticalGrid��5      $��Stretch=�6!   $��0,10,0,0q�6>    � RowDefinitions 5	   
   �� 5
      $��* �6   �� 5      $
��auto �6   �� 5      $
��auto �6   .+�5   
   � -   5        $ HorizontalGrid��5      $��Stretch=�6'   $��0��6E    � ColumnDefinitions 5      �� 5      $
��auto �6#   �� 5      $��* �6#   .+�5      �� $��0��6   ���	#FF555555$��0 0 0 1q�6=    �� Padding$ 0q�6W    J� SnapsToDevicePixels . ._�5      �� $��10 0 5 0q�6   .+�5      �� -   5         ��Name$ OpenFileButton��5      $
��Open��65   $��	0,0,0,2.5q�6a      
DarkButton#���   6t   	 � ToolTip$	 Open a .mid or .mus file��6�   �� 5      -   5        $ SaveFileButton��5      $
��Save��65   $��
0,2.5,0,10q�6D   {�.  #���   6j   $9	 3Save the currently loaded song in .mid or .mus file��6�   �� 5      -   5        $ BatchConvertButton��5      $��Convert Batch��69   $
��0,10q�6Q   {�. #���   6p   $|	 vSelect a folder with multiple .mid files and convert them all into one .mus files (usually used for creating datasets)��6�   �� 5      -   5        $ 
PlayButton��5      $
��Play��61   $��
0,10,0,2.5q�6@   {�.  #���   6   $��32q�6�   �� 5      -   5        $ 
StopButton��5      $
��Stop��61   $��
0,2.5,0,10q�6@   {�.  #���   6   �� 5      -	   5        $ SettingsButton��5      $��Settings��65   $��0,10,0,5q�6H   #���   6w   5      5      �� 5      $��1��6   ���	#FF555555$��1 1 0 0q�6=   $ 0q�6W    . ._�5      � -
   5        $ 	ImageGrid��5      $��0q�6*    5       �� 5!      $��* �6(   �� 5"      $
��auto �6(   .+�5%      �� -   5        
 ��Name$
 PianoRollImage��5%      $��0��64   $
��Left=�6A   $	��Top=�6\   $
��None=�6t   � 5&      -   5        $ ChannelButtonsGrid��5&      $��1��67    5'      �� 5(      $��* �6/   �� 5)      $��* �6/   �� 5*      $��* �6/   �� 5+      $��* �6/   �� 5,      $��* �6/   �� 5-      $��* �6/   �� 5.      $��* �6/   �� 5/      $��* �6/   �� 50      $��* �6/   �� 51      $��* �6/   �� 52      $��* �6/   �� 53      $��* �6/   �� 54      $��* �6/   �� 55      $��* �6/   �� 56      $��* �6/   �� 57      $��* �6/   5:      5;      5<      5=      �� 5>   
   -   5         ��Name$ OpenedFilePathLabel��5>      $��2��6-   $��Label��6:   $��10,0,5,0q�6J   $	��0,5q�6\   ���	#FF777777$��10(�6�   5@      5A      