<?xml version="1.0"?>
<Scenario xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" UID="9016309d2f0944a59eba3a27c3dcc7ac" SceName="CASE2" SceType="DynamicGas" IniState="CMBSTEOPF" StartTime="2016-12-01T06:00:00" EndTime="2016-12-02T06:00:00" dT="900" Comment="" NetType="GAS" NetName="GNET25" NetFilePath="C:\Users\KP\Source\Workspaces\SAInt Software\SAInt-CM\Network\PaperAppliedEnergy\GNET25.net" Version="1.2.1.0" TimeCreated="2016-12-03T02:13:30.4180306+01:00" FilePath="C:\Users\KP\Source\Workspaces\SAInt Software\SAInt-CM\Network\PaperAppliedEnergy\CASE2.sce" TR="180" SingleCnstrHandling="false">
  <SimCtrlList />
  <SceProfiles>
    <SceProfile UID="4c7c124b01794132b1c83a8c99cd8894" TimeStep="1" Name="PROFILE1" PrflType="Deterministic" InterpolationType="Cubic" DistributionType="Uniform" DurationType="Periodic" Sign="true" DataSource="" UseSource="false">
      <PRFDATA>
        <DATA Mean="1" Deviatoin="0.01" />
        <DATA Mean="1.025" Deviatoin="0.01" />
        <DATA Mean="1.125" Deviatoin="0.01" />
        <DATA Mean="1.2" Deviatoin="0.01" />
        <DATA Mean="1.25" Deviatoin="0.01" />
        <DATA Mean="1.2" Deviatoin="0.01" />
        <DATA Mean="1.125" Deviatoin="0.01" />
        <DATA Mean="1.115" Deviatoin="0.01" />
        <DATA Mean="1.1" Deviatoin="0.01" />
        <DATA Mean="1.15" Deviatoin="0.01" />
        <DATA Mean="1.175" Deviatoin="0.01" />
        <DATA Mean="1.2" Deviatoin="0.01" />
        <DATA Mean="1.175" Deviatoin="0.01" />
        <DATA Mean="1.15" Deviatoin="0.01" />
        <DATA Mean="1.25" Deviatoin="0.01" />
        <DATA Mean="1.45" Deviatoin="0.01" />
        <DATA Mean="1.5" Deviatoin="0.01" />
        <DATA Mean="1.45" Deviatoin="0.01" />
        <DATA Mean="1.35" Deviatoin="0.01" />
        <DATA Mean="1.275" Deviatoin="0.01" />
        <DATA Mean="1.225" Deviatoin="0.01" />
        <DATA Mean="1.175" Deviatoin="0.01" />
        <DATA Mean="1.1" Deviatoin="0.01" />
        <DATA Mean="1.05" Deviatoin="0.01" />
        <DATA Mean="1" Deviatoin="0.01" />
      </PRFDATA>
    </SceProfile>
    <SceProfile UID="50b633e3ccc744adbbb276f2bee6b301" TimeStep="6" Name="PROFILE2" PrflType="Deterministic" InterpolationType="Linear" DistributionType="Normal" DurationType="Constant" Sign="true" DataSource="" UseSource="false">
      <PRFDATA>
        <DATA Mean="1" Deviatoin="0.01" />
        <DATA Mean="0.5" Deviatoin="0.01" />
        <DATA Mean="0.5" Deviatoin="0" />
        <DATA Mean="0.5" Deviatoin="0" />
      </PRFDATA>
    </SceProfile>
    <SceProfile UID="5f70b69e44b440219b5dce7acb5b7549" TimeStep="1" Name="PROFILE3" PrflType="Deterministic" InterpolationType="Linear" DistributionType="Exponential" DurationType="Stop" Sign="true" DataSource="" UseSource="false">
      <PRFDATA>
        <DATA Mean="1" Deviatoin="0.01" />
        <DATA Mean="1.025" Deviatoin="0.01" />
        <DATA Mean="1.125" Deviatoin="0.01" />
        <DATA Mean="1.2" Deviatoin="0.01" />
        <DATA Mean="1.25" Deviatoin="0.01" />
        <DATA Mean="1.2" Deviatoin="0.01" />
        <DATA Mean="1.125" Deviatoin="0.01" />
        <DATA Mean="1.115" Deviatoin="0.01" />
        <DATA Mean="1.1" Deviatoin="0.01" />
        <DATA Mean="1.15" Deviatoin="0.01" />
        <DATA Mean="1.175" Deviatoin="0.01" />
        <DATA Mean="1.2" Deviatoin="0.01" />
        <DATA Mean="1.175" Deviatoin="0.01" />
        <DATA Mean="1.15" Deviatoin="0.01" />
        <DATA Mean="1.25" Deviatoin="0.01" />
        <DATA Mean="1.45" Deviatoin="0.01" />
        <DATA Mean="1.5" Deviatoin="0.01" />
        <DATA Mean="1.45" Deviatoin="0.01" />
        <DATA Mean="1.35" Deviatoin="0.01" />
        <DATA Mean="1.275" Deviatoin="0.01" />
        <DATA Mean="1.225" Deviatoin="0.01" />
        <DATA Mean="1.175" Deviatoin="0.01" />
        <DATA Mean="1.1" Deviatoin="0.01" />
        <DATA Mean="1.05" Deviatoin="0.01" />
        <DATA Mean="1" Deviatoin="0.01" />
      </PRFDATA>
    </SceProfile>
  </SceProfiles>
  <SceEvents>
    <SceEvent Active="true" UID="78e8b0033f284dca90fd5cc1bbf5304d" ObjTyp="NO" ObjName="N11" Info="-" Parameter="QSET" EvtTime="2016-12-01T06:00:00" Value="5.5555555555555554" ProfileName="PROFILE1" EvalType="NONE">
      <Condition Expression="">
        <ExprValues />
      </Condition>
      <ValueExpression Expression="">
        <ExprValues />
      </ValueExpression>
    </SceEvent>
    <SceEvent Active="true" UID="2c1d8a79a4f9485793a76ae57b505359" ObjTyp="NO" ObjName="N15" Info="-" Parameter="QSET" EvtTime="2016-12-01T06:00:00" Value="8.3333333333333339" ProfileName="PROFILE1" EvalType="NONE">
      <Condition Expression="">
        <ExprValues />
      </Condition>
      <ValueExpression Expression="">
        <ExprValues />
      </ValueExpression>
    </SceEvent>
    <SceEvent Active="true" UID="c3d4c28ab3704ba594fe838e17044804" ObjTyp="NO" ObjName="N14" Info="-" Parameter="QSET" EvtTime="2016-12-01T06:00:00" Value="11.111111111111111" ProfileName="PROFILE1" EvalType="NONE">
      <Condition Expression="">
        <ExprValues />
      </Condition>
      <ValueExpression Expression="">
        <ExprValues />
      </ValueExpression>
    </SceEvent>
    <SceEvent Active="true" UID="d83b0a967fe74eeda1e5fe91fd90e775" ObjTyp="NO" ObjName="N24" Info="-" Parameter="QSET" EvtTime="2016-12-01T06:00:00" Value="41.666666666666664" ProfileName="PROFILE1" EvalType="NONE">
      <Condition Expression="">
        <ExprValues />
      </Condition>
      <ValueExpression Expression="">
        <ExprValues />
      </ValueExpression>
    </SceEvent>
    <SceEvent Active="true" UID="1888cea475f44eee81fa990f309a28ef" ObjTyp="NO" ObjName="N21" Info="-" Parameter="QSET" EvtTime="2016-12-01T06:00:00" Value="27.777777777777779" ProfileName="PROFILE1" EvalType="NONE">
      <Condition Expression="">
        <ExprValues />
      </Condition>
      <ValueExpression Expression="">
        <ExprValues />
      </ValueExpression>
    </SceEvent>
    <SceEvent Active="true" UID="73a4204379c04321b13093ff83df2352" ObjTyp="NO" ObjName="N20" Info="-" Parameter="QSET" EvtTime="2016-12-01T06:00:00" Value="16.666666666666668" ProfileName="PROFILE1" EvalType="NONE">
      <Condition Expression="">
        <ExprValues />
      </Condition>
      <ValueExpression Expression="">
        <ExprValues />
      </ValueExpression>
    </SceEvent>
    <SceEvent Active="true" UID="c4330e23430a4e4f9404d5fbabc865e1" ObjTyp="CS" ObjName="CS1" Info="-" Parameter="OFF" EvtTime="2016-12-01T14:00:00" Value="NaN" ProfileName="" EvalType="NONE">
      <Condition Expression="">
        <ExprValues />
      </Condition>
      <ValueExpression Expression="">
        <ExprValues />
      </ValueExpression>
    </SceEvent>
    <SceEvent Active="true" UID="c4330e23430a4e4f9404d5fbabc865e1" ObjTyp="CS" ObjName="CS1" Info="-" Parameter="BP" EvtTime="2016-12-01T22:00:00" Value="NaN" ProfileName="" EvalType="NONE">
      <Condition Expression="">
        <ExprValues />
      </Condition>
      <ValueExpression Expression="">
        <ExprValues />
      </ValueExpression>
    </SceEvent>
    <SceEvent Active="true" UID="c4330e23430a4e4f9404d5fbabc865e1" ObjTyp="CS" ObjName="CS1" Info="-" Parameter="POSET" EvtTime="2016-12-02T02:00:00" Value="6101324.9999999991" ProfileName="" EvalType="NONE">
      <Condition Expression="">
        <ExprValues />
      </Condition>
      <ValueExpression Expression="">
        <ExprValues />
      </ValueExpression>
    </SceEvent>
    <SceEvent Active="false" UID="9501f5a1cb554c2f96a023f9ac7ce8eb" ObjTyp="NO" ObjName="N22" Info="-" Parameter="PSET" EvtTime="2016-12-01T06:00:00" Value="101325" ProfileName="" EvalType="DoIFTRUE">
      <Condition Expression="GSUB.EAST.LP.[Msm3] &lt; 3.3">
        <ExprValues>
          <ExprValue Name="EAST" ObjType="GSUB" Str="GSUB.EAST.LP.[Msm3]" UID="9a3297dc35494902bacf071e2faf54cd" Extension="LP" Unit="Msm3" Time="NaN" />
        </ExprValues>
      </Condition>
      <ValueExpression Expression="NO.22.p.(gtime-gdt)*1.05">
        <ExprValues>
          <ExprValue Name="N22" ObjType="NO" Str="NO.22.p.(gtime-gdt)" UID="9501f5a1cb554c2f96a023f9ac7ce8eb" Extension="P" Unit="NO" Time="NaN" />
        </ExprValues>
      </ValueExpression>
    </SceEvent>
    <SceEvent Active="false" UID="9501f5a1cb554c2f96a023f9ac7ce8eb" ObjTyp="NO" ObjName="N22" Info="-" Parameter="PSET" EvtTime="2016-12-01T06:00:00" Value="5601325" ProfileName="" EvalType="DoIFTRUE">
      <Condition Expression="GSUB.EAST.LP.[Msm3] &gt;= 3.3">
        <ExprValues>
          <ExprValue Name="EAST" ObjType="GSUB" Str="GSUB.EAST.LP.[Msm3]" UID="9a3297dc35494902bacf071e2faf54cd" Extension="LP" Unit="Msm3" Time="NaN" />
        </ExprValues>
      </Condition>
      <ValueExpression Expression="">
        <ExprValues />
      </ValueExpression>
    </SceEvent>
    <SceEvent Active="false" UID="8b6e6a9b0e394a5e824b8952a4d70738" ObjTyp="NO" ObjName="N08" Info="-" Parameter="QSET" EvtTime="2016-12-01T06:00:00" Value="0" ProfileName="" EvalType="DoIFTRUE">
      <Condition Expression="NO.24.P.[barg] &lt; 20">
        <ExprValues>
          <ExprValue Name="N24" ObjType="NO" Str="NO.24.P.[barg]" UID="d83b0a967fe74eeda1e5fe91fd90e775" Extension="P" Unit="barg" Time="NaN" />
        </ExprValues>
      </Condition>
      <ValueExpression Expression="NO.8.Q.(gtime-gdt)*.9">
        <ExprValues>
          <ExprValue Name="N08" ObjType="NO" Str="NO.8.Q.(gtime-gdt)" UID="8b6e6a9b0e394a5e824b8952a4d70738" Extension="Q" Unit="NO" Time="NaN" />
        </ExprValues>
      </ValueExpression>
    </SceEvent>
    <SceEvent Active="false" UID="8b6e6a9b0e394a5e824b8952a4d70738" ObjTyp="NO" ObjName="N08" Info="-" Parameter="QSET" EvtTime="2016-12-01T06:00:00" Value="69.444444444444443" ProfileName="" EvalType="DoIFTRUE">
      <Condition Expression="NO.24.P.[barg] &gt; 30">
        <ExprValues>
          <ExprValue Name="N24" ObjType="NO" Str="NO.24.P.[barg]" UID="d83b0a967fe74eeda1e5fe91fd90e775" Extension="P" Unit="barg" Time="NaN" />
        </ExprValues>
      </Condition>
      <ValueExpression Expression="NO.8.q.(0)">
        <ExprValues>
          <ExprValue Name="N08" ObjType="NO" Str="NO.8.q.(0)" UID="8b6e6a9b0e394a5e824b8952a4d70738" Extension="Q" Unit="NO" Time="0" />
        </ExprValues>
      </ValueExpression>
    </SceEvent>
    <SceEvent Active="true" UID="9501f5a1cb554c2f96a023f9ac7ce8eb" ObjTyp="NO" ObjName="N22" Info="-" Parameter="PSET" EvtTime="2016-12-01T06:00:00" Value="6101324.9999999991" ProfileName="" EvalType="DoIFTRUE">
      <Condition Expression="GSUB.EAST.LP.[Msm3] &lt; 3.3">
        <ExprValues>
          <ExprValue Name="EAST" ObjType="GSUB" Str="GSUB.EAST.LP.[Msm3]" UID="9a3297dc35494902bacf071e2faf54cd" Extension="LP" Unit="Msm3" Time="NaN" />
        </ExprValues>
      </Condition>
      <ValueExpression Expression="">
        <ExprValues />
      </ValueExpression>
    </SceEvent>
    <SceEvent Active="true" UID="9501f5a1cb554c2f96a023f9ac7ce8eb" ObjTyp="NO" ObjName="N22" Info="-" Parameter="WDR" EvtTime="2016-12-01T06:00:00" Value="NaN" ProfileName="" EvalType="NONE">
      <Condition Expression="">
        <ExprValues />
      </Condition>
      <ValueExpression Expression="">
        <ExprValues />
      </ValueExpression>
    </SceEvent>
  </SceEvents>
</Scenario>