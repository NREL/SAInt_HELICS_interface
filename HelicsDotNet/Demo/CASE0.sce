<?xml version="1.0"?>
<Scenario xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" UID="5091573ea6594327a0ae48b17a660647" SceName="CASE0" SceType="DynamicGas" SolverStatus="Solved" IniState="CMBSTEOPF" StartTime="2016-12-01T06:00:00" EndTime="2016-12-02T06:00:00" dT="900" Comment="" NetType="GAS" NetName="GNET25" NetFilePath="C:\Users\KP\Documents\GitHub\SAInt_HELICS_interface\HelicsDotNet\Demo\GNET25.net" Version="2.0.25.0" TimeCreated="2016-12-03T02:13:30.4180306+01:00" TimeModified="2021-04-13T19:17:44.8354345+02:00" FilePath="C:\Users\KP\Documents\GitHub\SAInt_HELICS_interface\HelicsDotNet\Demo\CASE0.sce" TR="180" SingleCnstrHandling="false">
  <SceEvents>
    <SceEvent Active="true" Info="-" UID="78e8b0033f284dca90fd5cc1bbf5304d" EvtTime="2016-12-01T06:00:00" Parameter="QSET" Value="5.5555555555555554" EvalType="NONE" ProfileName="DEMANDPRF">
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
    <SceEvent Active="true" Info="-" UID="2c1d8a79a4f9485793a76ae57b505359" EvtTime="2016-12-01T06:00:00" Parameter="QSET" Value="8.3333333333333339" EvalType="NONE" ProfileName="DEMANDPRF">
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
    <SceEvent Active="true" Info="-" UID="c3d4c28ab3704ba594fe838e17044804" EvtTime="2016-12-01T06:00:00" Parameter="QSET" Value="11.111111111111111" EvalType="NONE" ProfileName="DEMANDPRF">
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
    <SceEvent Active="true" Info="-" UID="d83b0a967fe74eeda1e5fe91fd90e775" EvtTime="2016-12-01T06:00:00" Parameter="QSET" Value="41.666666666666664" EvalType="NONE" ProfileName="DEMANDPRF">
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
    <SceEvent Active="true" Info="-" UID="1888cea475f44eee81fa990f309a28ef" EvtTime="2016-12-01T06:00:00" Parameter="QSET" Value="27.777777777777779" EvalType="NONE" ProfileName="DEMANDPRF">
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
    <SceEvent Active="true" Info="-" UID="73a4204379c04321b13093ff83df2352" EvtTime="2016-12-01T06:00:00" Parameter="QSET" Value="16.666666666666668" EvalType="NONE" ProfileName="DEMANDPRF">
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
  </SceEvents>
  <SceProfiles>
    <SceProfile UID="4eb1ed1ead15490885f5178c7831a3c8" TimeStep="1" MinVal="-1E+20" MaxVal="1E+20" BaseDev="0" Name="DEMANDPRF" PrflType="Deterministic" InterpolationType="Cubic" DistributionType="Uniform" DurationType="Periodic" Sign="true" DevAtMean="true" DevAtMin="true" DevAtMax="true" UseBounds="false" UseBaseDev="false" DataSource="" UseSource="false" Info="-">
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
    <SceProfile UID="25d5b93b64e8418fbe49a46a2c4a664c" TimeStep="6" MinVal="-1E+20" MaxVal="1E+20" BaseDev="0" Name="PROFILE2" PrflType="Deterministic" InterpolationType="Linear" DistributionType="Normal" DurationType="Constant" Sign="true" DevAtMean="true" DevAtMin="true" DevAtMax="true" UseBounds="false" UseBaseDev="false" DataSource="" UseSource="false" Info="-">
      <PRFDATA>
        <DATA Mean="1" Deviatoin="0.01" />
        <DATA Mean="0.5" Deviatoin="0.01" />
        <DATA Mean="0.5" Deviatoin="0" />
        <DATA Mean="0.5" Deviatoin="0" />
      </PRFDATA>
    </SceProfile>
    <SceProfile UID="795fb40b01ca48b58046e3934335eeab" TimeStep="1" MinVal="-1E+20" MaxVal="1E+20" BaseDev="0" Name="PROFILE3" PrflType="Deterministic" InterpolationType="Linear" DistributionType="Exponential" DurationType="Stop" Sign="true" DevAtMean="true" DevAtMin="true" DevAtMax="true" UseBounds="false" UseBaseDev="false" DataSource="" UseSource="false" Info="-">
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
</Scenario>