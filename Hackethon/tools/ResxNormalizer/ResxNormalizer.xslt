<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" indent="yes" omit-xml-declaration="no" encoding="utf-8"/>

	<!-- Identity template: copy everything by default -->
	<xsl:template match="@*|node()">
		<xsl:copy>
			<xsl:apply-templates select="@*|node()"/>
		</xsl:copy>
	</xsl:template>

	<!-- Special handling for root element -->
	<xsl:template match="/*">
		<xsl:copy>
			<xsl:apply-templates select="@*"/>

			<!-- Copy all non-data elements first (metadata, resheader, etc.) -->
			<xsl:apply-templates select="node()[not(self::data)]"/>

			<!-- Then add sorted data elements -->
			<xsl:apply-templates select="data">
				<xsl:sort select="@name" data-type="text" order="ascending"/>
			</xsl:apply-templates>
		</xsl:copy>
	</xsl:template>

	<!-- Ensure xml:space="preserve" on data elements -->
	<xsl:template match="data">
		<xsl:copy>
			<xsl:apply-templates select="@*"/>
			<xsl:if test="not(@xml:space)">
				<xsl:attribute name="xml:space">preserve</xsl:attribute>
			</xsl:if>
			<xsl:apply-templates select="node()"/>
		</xsl:copy>
	</xsl:template>

</xsl:stylesheet>