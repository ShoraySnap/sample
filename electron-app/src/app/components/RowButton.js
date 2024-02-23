import styled from "styled-components";

const Container = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding-right: 10px;
  padding-left: 10px;
  padding-top: 20px;
  margin-bottom: 45px;
  margin-right: 10px;
  margin-left: 10px;
  border-radius: 0.75rem;
  
  width: 150px;
  
  .enabled{
    cursor: pointer;
  }
  
  .disabled{
    cursor: not-allowed;
  }
  
  &:hover {
    background-color: #F2F2F2;
    font-weight: 700;
  }
`;

const InnerContainer = styled.div`
  width: inherit;
  height: inherit;
`;

const ButtonIcon = styled.img`
  padding-top: 5px;
  width: 50px;
  height: 50px;
`;

const Text = styled.p`
  font-size: 12px;
  font-weight: 600;
  font-style: normal;
  color: #2D2D2E;
  // letter-spacing: -0.333565px;
`;

const RowButton = ({title, icon, onClick, isDisabled}) => {
  
  const className = isDisabled ? "disabled" : "enabled";
  
  return (
    <Container>
      <InnerContainer className={className} onClick={isDisabled ? null : onClick}>
        <ButtonIcon  src={icon} alt={title} />
        <Text>{title}</Text>
      </InnerContainer>
    </Container>
  )
  
};

export default RowButton;