<?xml version="1.0"?>
<Scenario xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" UID="d5a121a9036c49b0b1bee96119e86c9b" SceName="CASE1" SceType="DynamicGas" SolverStatus="none" IniState="CMBSTEOPF" StartTime="2016-12-01T06:00:00" EndTime="2016-12-02T06:00:00" dT="900" Comment="" NetType="GAS" NetName="GNET25" NetFilePath="C:\Users\KP\Documents\GitHub\SAInt_HELICS_interface\HelicsDotNet\Demo\GNET25.net" Version="2.0.28.0" TimeCreated="2016-12-03T02:13:30.4180306+01:00" TimeModified="2021-08-03T20:18:47.7252769+02:00" FilePath="C:\Users\KP\Documents\GitHub\SAInt_HELICS_interface\HelicsDotNet\Demo\CASE1.sce" TR="180" SingleCnstrHandling="false">
  <SceEvents>
    <SceEvent Active="true" Info="-" UID="78e8b0033f284dca90fd5cc1bbf5304d" EvtTime="2016-12-01T06:00:00" Parameter="QSET" Value="5.5555555555555554" EvalType="NONE" ProfileName="PROFILE1">
      <ValueExpression>
        <Expression />
        <ExprValues />
      </ValueExpression>
      <Unit UnitName="ksm3_h" UnitType="Q" />
      <Condition>
        <Expression />
        <ExprValues />
      </Condition>
    </SceEvent>
    <SceEvent Active="true" Info="-" UID="2c1d8a79a4f9485793a76ae57b505359" EvtTime="2016-12-01T06:00:00" Parameter="QSET" Value="0" EvalType="NONE" ProfileName="">
      <ValueExpression>
        <Expression />
        <ExprValues />
      </ValueExpression>
      <Unit UnitName="ksm3_h" UnitType="Q" />
      <Condition>
        <Expression />
        <ExprValues />
      </Condition>
    </SceEvent>
    <SceEvent Active="false" Info="-" UID="c3d4c28ab3704ba594fe838e17044804" EvtTime="2016-12-01T06:00:00" Parameter="QSET" Value="11.111111111111111" EvalType="NONE" ProfileName="PROFILE1">
      <ValueExpression>
        <Expression />
        <ExprValues />
      </ValueExpression>
      <Unit UnitName="ksm3_h" UnitType="Q" />
      <Condition>
        <Expression />
        <ExprValues />
      </Condition>
    </SceEvent>
    <SceEvent Active="true" Info="-" UID="d83b0a967fe74eeda1e5fe91fd90e775" EvtTime="2016-12-01T06:00:00" Parameter="QSET" Value="41.666666666666664" EvalType="NONE" ProfileName="PROFILE1">
      <ValueExpression>
        <Expression />
        <ExprValues />
      </ValueExpression>
      <Unit UnitName="ksm3_h" UnitType="Q" />
      <Condition>
        <Expression />
        <ExprValues />
      </Condition>
    </SceEvent>
    <SceEvent Active="true" Info="-" UID="1888cea475f44eee81fa990f309a28ef" EvtTime="2016-12-01T06:00:00" Parameter="QSET" Value="0" EvalType="NONE" ProfileName="">
      <ValueExpression>
        <Expression />
        <ExprValues />
      </ValueExpression>
      <Unit UnitName="ksm3_h" UnitType="Q" />
      <Condition>
        <Expression />
        <ExprValues />
      </Condition>
    </SceEvent>
    <SceEvent Active="true" Info="-" UID="73a4204379c04321b13093ff83df2352" EvtTime="2016-12-01T06:00:00" Parameter="QSET" Value="0" EvalType="NONE" ProfileName="">
      <ValueExpression>
        <Expression />
        <ExprValues />
      </ValueExpression>
      <Unit UnitName="ksm3_h" UnitType="Q" />
      <Condition>
        <Expression />
        <ExprValues />
      </Condition>
    </SceEvent>
    <SceEvent Active="true" Info="-" UID="c4330e23430a4e4f9404d5fbabc865e1" EvtTime="2016-12-01T14:00:00" Parameter="OFF" Value="NaN" EvalType="NONE" ProfileName="">
      <ValueExpression>
        <Expression />
        <ExprValues />
      </ValueExpression>
      <Unit UnitName="NO" UnitType="NO" />
      <Condition>
        <Expression />
        <ExprValues />
      </Condition>
    </SceEvent>
    <SceEvent Active="true" Info="-" UID="c4330e23430a4e4f9404d5fbabc865e1" EvtTime="2016-12-01T22:00:00" Parameter="BP" Value="NaN" EvalType="NONE" ProfileName="">
      <ValueExpression>
        <Expression />
        <ExprValues />
      </ValueExpression>
      <Unit UnitName="ND" UnitType="ND" />
      <Condition>
        <Expression />
        <ExprValues />
      </Condition>
    </SceEvent>
    <SceEvent Active="true" Info="-" UID="c4330e23430a4e4f9404d5fbabc865e1" EvtTime="2016-12-02T02:00:00" Parameter="POSET" Value="6101324.9999999991" EvalType="NONE" ProfileName="">
      <ValueExpression>
        <Expression />
        <ExprValues />
      </ValueExpression>
      <Unit UnitName="barg" UnitType="P" />
      <Condition>
        <Expression />
        <ExprValues />
      </Condition>
    </SceEvent>
  </SceEvents>
  <SceProfiles>
    <SceProfile UID="bddf0a76e30d42b89503be9c822be7fc" TimeStep="1" MinVal="-1E+20" MaxVal="1E+20" BaseDev="0" Name="PROFILE1" PrflType="Deterministic" InterpolationType="Cubic" DistributionType="Uniform" DurationType="Periodic" Sign="true" DevAtMean="true" DevAtMin="true" DevAtMax="true" UseBounds="false" UseBaseDev="false" DataSource="" UseSource="false" Info="-">
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
    <SceProfile UID="4636b8ae5ba4492b97a3ca574d4a0176" TimeStep="6" MinVal="-1E+20" MaxVal="1E+20" BaseDev="0" Name="PROFILE2" PrflType="Deterministic" InterpolationType="Linear" DistributionType="Normal" DurationType="Constant" Sign="true" DevAtMean="true" DevAtMin="true" DevAtMax="true" UseBounds="false" UseBaseDev="false" DataSource="" UseSource="false" Info="-">
      <PRFDATA>
        <DATA Mean="1" Deviatoin="0.01" />
        <DATA Mean="0.5" Deviatoin="0.01" />
        <DATA Mean="0.5" Deviatoin="0" />
        <DATA Mean="0.5" Deviatoin="0" />
      </PRFDATA>
    </SceProfile>
    <SceProfile UID="19223e92dc6a48ef8718cbcdf5a11f77" TimeStep="1" MinVal="-1E+20" MaxVal="1E+20" BaseDev="0" Name="PROFILE3" PrflType="Deterministic" InterpolationType="Linear" DistributionType="Exponential" DurationType="Stop" Sign="true" DevAtMean="true" DevAtMin="true" DevAtMax="true" UseBounds="false" UseBaseDev="false" DataSource="" UseSource="false" Info="-">
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
    <SceProfile UID="f97b505cc4b4470cb0725788f419948c" TimeStep="4" MinVal="-1E+20" MaxVal="1E+20" BaseDev="0" Name="PRFL" PrflType="Stochastic" InterpolationType="Linear" DistributionType="Normal" DurationType="Periodic" Sign="false" DevAtMean="true" DevAtMin="true" DevAtMax="true" UseBounds="false" UseBaseDev="false" DataSource="" UseSource="false" Info="-">
      <PRFDATA>
        <DATA Mean="1" Deviatoin="0.5" />
        <DATA Mean="1.2" Deviatoin="0.5" />
        <DATA Mean="0.8" Deviatoin="0.5" />
        <DATA Mean="1" Deviatoin="0.5" />
      </PRFDATA>
    </SceProfile>
  </SceProfiles>
</Scenario>